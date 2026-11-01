using System.Text.Json;
using FastEndpoints;
using PaymentsDemo.Api.Infrastructure.Data;
using PaymentsDemo.Api.Infrastructure.Services.AcmeService;
using PaymentsDemo.Api.Shared.Responses;

namespace PaymentsDemo.Api.Features.Payments.Endpoints.SubmitPayment;

public sealed class SubmitPaymentEndpoint(
    ApplicationDbContext db,
    IAcmeService acmeService) : Endpoint<PaymentIntent>
{
    public override void Configure()
    {
        Post(RouteConstants.SubmitPayment);
        AllowAnonymous(); // add your own auth for callers of this endpoint
        Summary(s =>
        {
            s.Summary = "Submit a payment to AcmePay";
            s.Description = "Validates the envelope and forwards it to AcmePay v1 /v1/api/payment.";
            s.Response<PaymentResponseEnvelope>(200, "Payment processed by AcmePay");
            s.Response(400, "Validation failed");
            s.Response(502, "AcmePay returned an error or was unreachable");
        });
    }

    public override async Task HandleAsync(PaymentIntent req, CancellationToken ct)
    {
        var statusCode = await acmeService.SubmitPaymentAsync(req, ct);

        await Send.StringAsync(JsonSerializer.Serialize(paymentResponse), (int)statusCode, "application/json", ct);
    }
}