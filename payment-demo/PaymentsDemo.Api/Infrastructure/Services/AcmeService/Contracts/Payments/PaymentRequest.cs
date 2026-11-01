namespace PaymentsDemo.Api.Infrastructure.Services.AcmeService.Contracts.Payments;

public record PaymentRequest(
    string MerchantId,
    string TransactionId,
    string OrderNumber,
    long Amount,
    string PaymentService,             // CardNotPresent | CardNotPresentRecurring | AcmeEft | GooglePay | ApplePay
    Card? Card = null,
    string? CurrencyCode = null,       // optional, Acme defaults to ZAR
    string? CardOnFileScenario = null, // CIT | MIT
    string? SubMerchantId = null,
    string? MerchantCategoryCode = null,
    string? CardAcceptorName = null,
    string? Email = null
);