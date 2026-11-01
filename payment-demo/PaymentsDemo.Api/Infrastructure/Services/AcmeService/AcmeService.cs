using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using PaymentDemo.Features.Payments.Endpoints.SubmitPayment;
using PaymentsDemo.Api.Infrastructure.Configuration;
using PaymentsDemo.Api.Shared.Requests;

namespace PaymentsDemo.Api.Infrastructure.Services.AcmeService;

public interface IAcmeService
{
    Task<HttpStatusCode> SubmitPaymentAsync(PaymentIntent paymentIntent, CancellationToken ct);
}

public class AcmeService(IAmazonSQS queueClient, IOptions<AwsConfig> awsConfig) : IAcmeService
{
    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        PropertyNamingPolicy = null,                                  // PascalCase out
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // omit unset optionals
    };

    public async Task<HttpStatusCode> SubmitPaymentAsync(PaymentIntent paymentIntent, CancellationToken ct)
    {
        PaymentEnvelope<PaymentIntent> envelope = new(
            new MessageHeader(
                MessageDateTime: DateTimeOffset.UtcNow.AddHours(2).ToString("yyyy-MM-dd'T'HH:mm:ss.fff"), // SAST, no offset
                MessageId: Guid.NewGuid().ToString(),// unique per message
                Channel: "Web"
            ),
        paymentIntent);

        var queueUrlResponse = await queueClient.GetQueueUrlAsync(awsConfig.Value.AcmePaymentServiceIntentQueueName, ct);

        var message = new SendMessageRequest(queueUrlResponse.QueueUrl, JsonSerializer.Serialize(envelope));

        await queueClient.SendMessageAsync(message, ct);

        return HttpStatusCode.Accepted;
    }
}