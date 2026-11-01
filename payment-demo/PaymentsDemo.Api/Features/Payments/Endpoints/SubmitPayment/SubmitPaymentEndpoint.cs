using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FastEndpoints;
using PaymentsDemo.Api.Shared.Requests;

namespace PaymentDemo.Features.Payments.Endpoints.SubmitPayment;

// POST /api/payment -> forwards to Acme's POST /api/payment
public sealed class SubmitPaymentEndpoint(
    IHttpClientFactory httpFactory,
    IConfiguration config) : Endpoint<PaymentRequest>
{
    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        PropertyNamingPolicy = null,                                  // PascalCase out
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // omit unset optionals
    };

    public override void Configure()
    {
        Post("/api/payment");
        AllowAnonymous(); // add your own auth for callers of this endpoint
    }

    public override async Task HandleAsync(PaymentRequest req, CancellationToken ct)
    {
        PaymentEnvelope<PaymentRequest> envelope = new(
            new MessageHeader(
                MessageDateTime: DateTimeOffset.UtcNow.AddHours(2).ToString("yyyy-MM-dd'T'HH:mm:ss.fff"), // SAST, no offset
                MessageId: Guid.NewGuid().ToString(),// unique per message
                Channel: "Web"
            ),
        req);

        var http = httpFactory.CreateClient("acme");
        using var msg = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "api/payment")
        {
            Content = new StringContent(JsonSerializer.Serialize(envelope, WriteOpts), Encoding.UTF8, new MediaTypeHeaderValue("application/json")),
        };

        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config["ACME_API_KEY"]);

        using var resp = await http.SendAsync(msg, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        // Pass Acme's status through (400 E01, 401 E02, 409 E09, 429 E29, 5xx E50).
        // A 200 with a ResponseDetail.Code other than "00" means the bank declined it.
        if (resp.Headers.RetryAfter?.Delta is { } wait)
            HttpContext.Response.Headers.RetryAfter = ((int)wait.TotalSeconds).ToString();

        await Send.StringAsync(body, (int)resp.StatusCode, "application/json", ct);
    }
}