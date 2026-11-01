using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using PaymentsDemo.AcmeServiceMessageRelay.Infrastructure;
using Polly;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace PaymentsDemo.AcmeServiceMessageRelay;

public class Function
{
    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        var acmeBaseUrl = Environment.GetEnvironmentVariable("ACME_BASE_URL") 
            ?? throw new InvalidOperationException("Missing required environment variable: ACME_BASE_URL");
        
        var acmeApiKey = Environment.GetEnvironmentVariable("ACME_API_KEY") 
            ?? throw new InvalidOperationException("Missing required environment variable: ACME_API_KEY");
        
        _responseQueueUrl = Environment.GetEnvironmentVariable("RESPONSE_QUEUE_URL") 
            ?? throw new InvalidOperationException("Missing required environment variable: RESPONSE_QUEUE_URL");

        var services = new ServiceCollection();
        services.AddHttpClient("acme", client =>
            {
                client.BaseAddress = new Uri(acmeBaseUrl);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", acmeApiKey);
            })
            .AddStandardResilienceHandler(options =>
            {
                // Retried by default: 408, 429, 5xx and transport errors. 4xx such as 409 are not retried.
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.Delay = TimeSpan.FromSeconds(2);
                options.Retry.UseJitter = true;

                // Each attempt is capped, and the whole call including retries is capped.
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(8);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            });

        var provider = services.BuildServiceProvider();
        _acme = provider.GetRequiredService<IHttpClientFactory>().CreateClient("acme");
        _sqs = new AmazonSQSClient();
    }

    private readonly HttpClient _acme;
    private readonly IAmazonSQS _sqs;
    private readonly string _responseQueueUrl;

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
    /// to respond to SQS messages.
    /// </summary>
    /// <param name="evnt">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var message in evnt.Records)
        {
            await ProcessMessageAsync(message, context);
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        // The body is forwarded unchanged, so Acme receives exactly the envelope the API built.
        var transactionId = JsonNode.Parse(message.Body)?["PaymentRequest"]?["TransactionId"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Message has no PaymentRequest.TransactionId");

        using var content = new StringContent(message.Body, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        using var response = await _acme.PostAsync("/api/payment", content);
        var body = await response.Content.ReadAsStringAsync();
        var status = (int)response.StatusCode;

        context.Logger.LogInformation($"Acme responded {status} for TransactionId {transactionId}");

        switch (status)
        {
            case 200:
                await PublishAsync(FromPaymentResponse(transactionId, status, body), context);
                break;

            case 400:
                // Our message is malformed. Retrying won't help, so report it as rejected.
                await PublishAsync(FromError(transactionId, "Rejected", status, body), context);
                break;

            case 409:
                // The order prefix is already in use by another TransactionID. Permanent for this key.
                await PublishAsync(FromConflict(transactionId, status, body), context);
                break;

            case 401:
                // Our API key is wrong. This is a deployment problem, so throw and let it reach the DLQ and alarm.
                throw new InvalidOperationException("Acme rejected our API key (401)");

            default:
                // 429, 5xx or a timeout after the handler's retries. Throw so SQS redelivers later.
                throw new HttpRequestException($"Acme returned {status} after retries");
        }
    }

    private static PaymentResult FromPaymentResponse(string transactionId, int status, string body)
    {
        var pr = JsonNode.Parse(body)?["PaymentResponse"] ?? throw new InvalidOperationException("200 response without PaymentResponse");

        var detail = pr["ResponseDetail"];
        var code = detail?["Code"]?.GetValue<string>() ?? throw new InvalidOperationException("200 response without ResponseDetail.Code");

        return new PaymentResult
        {
            TransactionId = transactionId,
            Outcome = code == "00" ? "Approved" : "Declined",
            AcmeHttpStatus = status,
            AcmeResponseCode = code,
            ResponseDescription = detail?["Description"]?.GetValue<string>(),
            ReconId = pr["ReconID"]?.GetValue<string>(),
            AuthCode = pr["AuthCode"]?.GetValue<string>(),
            AuthAmount = pr["AuthAmount"]?.GetValue<long>(),
            ReceivedAtUtc = DateTime.UtcNow
        };
    }

    private static PaymentResult FromConflict(string transactionId, int status, string body)
    {
        var pr = JsonNode.Parse(body)?["PaymentResponse"];
        return new PaymentResult
        {
            TransactionId = transactionId,
            Outcome = "Rejected",
            AcmeHttpStatus = status,
            AcmeResponseCode = pr?["ResponseDetail"]?["Code"]?.GetValue<string>() ?? "E09",
            ResponseDescription = pr?["ResponseDetail"]?["Description"]?.GetValue<string>(),
            // Acme returns the TransactionId that already holds this order prefix. Confirm its location against the stub.
            ConflictingTransactionId = pr?["TransactionId"]?.GetValue<string>(),
            ReceivedAtUtc = DateTime.UtcNow
        };
    }

    private static PaymentResult FromError(string transactionId, string outcome, int status, string body)
    {
        var detail = JsonNode.Parse(body)?["PaymentResponse"]?["ResponseDetail"];
        return new PaymentResult
        {
            TransactionId = transactionId,
            Outcome = outcome,
            AcmeHttpStatus = status,
            AcmeResponseCode = detail?["Code"]?.GetValue<string>(),
            ResponseDescription = detail?["Description"]?.GetValue<string>(),
            ReceivedAtUtc = DateTime.UtcNow
        };
    }

    private async Task PublishAsync(PaymentResult result, ILambdaContext context)
    {
        await _sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _responseQueueUrl,
            MessageBody = JsonSerializer.Serialize(result)
        });

        context.Logger.LogInformation($"Published {result.Outcome} for {result.TransactionId}");
    }
}