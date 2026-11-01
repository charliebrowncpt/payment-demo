using PaymentsDemo.Api.Shared.Requests;

namespace PaymentsDemo.Api.Shared.Responses;

public record PaymentResponse(
    ResponseDetail ResponseDetail,
    string? TransactionId = null,
    DateTimeOffset? TransactionDateTimeUtc = null,
    string? OrderNumber = null,
    string? ReconId = null,                   // appears on the merchant's settlement file
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

public record ResponseDetail(
    string Code,                    // max 3 on payment responses ("PEND"/"COMPL" are the EFT exceptions)
    string? Source = null,          // Acquirer | EFT | 3DSecure | Application
    string? Description = null,     // max 50
    string? ClientMessage = null
); 

public static class Sast
{
    // Kind is Unspecified so System.Text.Json writes no "Z" or offset.
    public static DateTime Now() => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(2), DateTimeKind.Unspecified);

    public static string Stamp() => Now().ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture);
}