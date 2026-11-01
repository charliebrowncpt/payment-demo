using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using PaymentsDemo.Api.Api.Infrastructure.Configuration;
using PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.Models;

namespace PaymentsDemo.Api.Infrastructure.Services.AcmeService;

public interface IAcmeQueueService
{
    Task<HttpStatusCode> SubmitPaymentAsync(Payment paymentIntentMessage, CancellationToken ct);
}

public class AcmeQueueService(IAmazonSQS queueClient, IOptions<AwsConfig> awsConfig) : IAcmeQueueService
{
    public async Task<HttpStatusCode> SubmitPaymentAsync(Payment paymentIntentMessage, CancellationToken ct)
    {
        var queueUrlResponse = await queueClient.GetQueueUrlAsync(awsConfig.Value.AcmePaymentServiceIntentQueueName, ct);

        var message = new SendMessageRequest(
            queueUrlResponse.QueueUrl, 
            JsonSerializer.Serialize(paymentIntentMessage, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,                                  // PascalCase out
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // omit unset optionals
            }));

        await queueClient.SendMessageAsync(message, ct);

        return HttpStatusCode.Accepted;
    }
}