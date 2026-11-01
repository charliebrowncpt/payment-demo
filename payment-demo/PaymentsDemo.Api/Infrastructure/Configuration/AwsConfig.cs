using System.ComponentModel.DataAnnotations;

namespace PaymentsDemo.Api.Infrastructure.Configuration;
public class AwsConfig
{
    [Required] public required string Region { get; init; }
    [Required] public required bool UseLocalStack { get; init; }
    [Required] public required string ServiceUrl { get; init; }
    [Required] public required string AcmePaymentServiceIntentQueueName { get; init; }
}