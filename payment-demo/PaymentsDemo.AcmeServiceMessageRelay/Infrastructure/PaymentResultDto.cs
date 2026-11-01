namespace PaymentsDemo.AcmeServiceMessageRelay.Infrastructure;
public class PaymentResult
{
    public string TransactionId { get; set; } = "";
    public string Outcome { get; set; } = "";              // Approved | Declined | Rejected
    public int AcmeHttpStatus { get; set; }
    public string? AcmeResponseCode { get; set; }
    public string? ResponseDescription { get; set; }
    public string? ReconId { get; set; }
    public string? AuthCode { get; set; }
    public long? AuthAmount { get; set; }
    public string? ConflictingTransactionId { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
}