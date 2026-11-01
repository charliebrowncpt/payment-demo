using AcmePay.Shared;

namespace AcmePay.Features.Payments.SubmitPayment;

public record Card(string Token);

public record PaymentRequest(
    string MerchantId,
    string TransactionId,
    string OrderNumber,
    long Amount,
    string PaymentService,             // CardNotPresent | CardNotPresentRecurring | AcmeEft | GooglePay | ApplePay
    Card? Card = null,                 // required on card services
    string? CurrencyCode = null,       // optional, ZAR when omitted
    string? CardOnFileScenario = null, // CIT | MIT
    string? SubMerchantId = null,
    string? MerchantCategoryCode = null,
    string? CardAcceptorName = null,
    string? Email = null
);

// The request body: every request carries a MessageHeader alongside its payload.
public record PaymentRequestEnvelope(MessageHeader MessageHeader, PaymentRequest PaymentRequest);