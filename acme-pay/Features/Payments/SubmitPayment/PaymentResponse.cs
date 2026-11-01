using AcmePay.Shared;

namespace AcmePay.Features.Payments.SubmitPayment;

// Error responses use the same envelope with the reason in ResponseDetail, so everything
// except ResponseDetail is optional. On a 409, TransactionID is the transaction that
// already holds the Retrieval Reference Number, not the one that was submitted.
public record PaymentResponse(
    ResponseDetail ResponseDetail,
    string? TransactionID = null,
    DateTime? TransactionDateTime = null,
    string? OrderNumber = null,
    string? ReconID = null,                   // appears on the merchant's settlement file
    string? RetrievalReferenceNumber = null,
    string? AuthCode = null,
    string? TransactionStatus = null,         // Success | Failure | Reversed | Complete | Refunded
    long? AuthAmount = null
);                 // can be less than Amount on a partial authorisation
 
public record PaymentResponseEnvelope(MessageHeader MessageHeader, PaymentResponse PaymentResponse)
{
    // Acme returns its own MessageID rather than repeating the caller's.
    public static PaymentResponseEnvelope For(PaymentResponse response, string channel = "App") =>
        new(
                new MessageHeader(
                    Sast.Stamp(),
                    Guid.NewGuid().ToString(),
                    channel
                ), 
            response
        );
}