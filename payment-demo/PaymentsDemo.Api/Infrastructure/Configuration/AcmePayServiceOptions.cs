using System.ComponentModel.DataAnnotations;

namespace PaymentsDemo.Api.Api.Infrastructure.Configuration;

public class AcmePayServiceOptions
{
    [Required] public required string BaseUrl { get; init; }
    [Required] public required string ApiKey { get; init; }
}