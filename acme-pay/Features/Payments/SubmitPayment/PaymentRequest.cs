using AcmePay.Shared;

namespace AcmePay.Features.Payments.SubmitPayment;

public record Card(string Token);

public record PaymentRequest(
    string MerchantID,
    string TransactionID,
    string OrderNumber,
    long Amount,
    string PaymentService,             // CardNotPresent | CardNotPresentRecurring | AcmeEft | GooglePay | ApplePay
    Card? Card = null,                 // required on card services
    string? CurrencyCode = null,       // optional, ZAR when omitted
    string? CardOnFileScenario = null, // CIT | MIT
    string? SubMerchantID = null,
    string? MerchantCategoryCode = null,
    string? CardAcceptorName = null,
    string? Email = null
);

// The request body: every request carries a MessageHeader alongside its payload.
public record PaymentRequestEnvelope(MessageHeader MessageHeader, PaymentRequest PaymentRequest);