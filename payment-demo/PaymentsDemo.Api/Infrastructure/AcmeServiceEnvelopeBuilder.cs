using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.SubMerchants;

namespace PaymentsDemo.Api.Infrastructure.Acme;

/// <summary>
/// Bound from the "Acme" configuration section. From the environment, set Acme__MerchantId.
/// </summary>
public sealed class AcmeOptions
{
    public const string SectionName = "Acme";

    /// <summary>The GUID Acme issued to Zapper's account. Server-side only, never from the client.</summary>
    public string MerchantId { get; set; } = string.Empty;
}

/// <summary>
/// Builds the Acme POST /api/payment request body. The result is stored on the payment and
/// forwarded unchanged by the relay Lambda. It contains the card token and email, so never log it.
/// </summary>
public sealed class AcmeServiceEnvelopeBuilder(IOptions<AcmeOptions> options)
{
    // Acme timestamps are South African Standard Time (UTC+02:00), ISO 8601, no offset, milliseconds.
    private static readonly TimeSpan Sast = TimeSpan.FromHours(2);
    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff";

    public string Build(
        string orderNumber,
        Guid transactionId,
        long amount,
        string cardToken,
        string email,
        SubMerchant sub,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(cardToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentNullException.ThrowIfNull(sub);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);

        var envelope = new JsonObject
        {
            ["MessageHeader"] = new JsonObject
            {
                // Invariant culture, so the separators can't change with the server's locale.
                ["MessageDateTime"] = now.ToOffset(Sast).ToString(TimestampFormat, CultureInfo.InvariantCulture),
                ["MessageID"] = Guid.NewGuid().ToString(),
                ["Channel"] = "App"
            },
            ["PaymentRequest"] = new JsonObject
            {
                ["MerchantID"] = options.Value.MerchantId,
                ["TransactionID"] = transactionId,
                ["OrderNumber"] = orderNumber,
                ["Amount"] = amount,
                ["CurrencyCode"] = "ZAR",
                ["PaymentService"] = "CardNotPresent",
                ["Card"] = new JsonObject { ["Token"] = cardToken },
                ["CardOnFileScenario"] = "CIT", // the customer is present and paying in the app
                ["SubMerchantID"] = sub.Code.Value,
                ["MerchantCategoryCode"] = sub.MerchantCategoryCode.Value,
                ["CardAcceptorName"] = sub.BuildCardAcceptorName().Value,
                ["Email"] = email
            }
        };

        return envelope.ToJsonString();
    }
}

public static class AcmeServiceCollectionExtensions
{
    /// Used to register from Program.cs. The AcmeEnvelopeBuilder is a singleton, and the options are validated at startup.
    public static IServiceCollection AddAcmeEnvelopeBuilder(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AcmeOptions>()
            .Bind(configuration.GetSection(AcmeOptions.SectionName))
            .Validate(o => Guid.TryParse(o.MerchantId, out _), "Acme:MerchantId must be a GUID.")
            .ValidateOnStart(); // fail at startup, not on the first payment

        services.AddSingleton<AcmeServiceEnvelopeBuilder>();
        return services;
    }
}