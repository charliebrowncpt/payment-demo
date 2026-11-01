using AcmePay.Features.Payments.SubmitPayment;

namespace AcmePay.Infrastructure;

public enum StoreOutcome { Created, Replayed, RrnInUse }
 
// Stands in for Acme's ledger. Register as a singleton.
public sealed class InMemoryPaymentStore
{
    private static readonly TimeSpan ReplayWindow = TimeSpan.FromHours(24);
 
    private sealed record Entry(PaymentResponse Response, DateTime CreatedUtc);
 
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Entry> _byTransactionId = [];
    private readonly Dictionary<string, string> _transactionIdByRrn = [];
 
    // Read-only. Outcome is null when neither the TransactionID nor the RRN has been seen.
    public (StoreOutcome? Outcome, PaymentResponse? Response, string TransactionId) Lookup(string transactionId, string rrn)
    {
        lock (_gate) return LookupLocked(transactionId, rrn);
    }
 
    // Replayed: the same TransactionID within 24 hours gets the stored outcome again.
    // RrnInUse: a different TransactionID already holds this RRN (Response is null; TransactionId is the holder).
    public (StoreOutcome Outcome, PaymentResponse? Response, string TransactionId) Submit(
        string transactionId, string rrn, Func<PaymentResponse> create)
    {
        lock (_gate)
        {
            var (Outcome, Response, TransactionId) = LookupLocked(transactionId, rrn);
            
            if (Outcome is { } outcome)
                return (outcome, Response, TransactionId);
 
            var response = create();
            _byTransactionId[transactionId] = new Entry(response, DateTime.UtcNow);
            _transactionIdByRrn[rrn] = transactionId;
            return (StoreOutcome.Created, response, transactionId);
        }
    }
 
    private (StoreOutcome? Outcome, PaymentResponse? Response, string TransactionId) LookupLocked(
        string transactionId, string rrn)
    {
        if (_byTransactionId.TryGetValue(transactionId, out var existing) &&
            DateTime.UtcNow - existing.CreatedUtc < ReplayWindow)
            return (StoreOutcome.Replayed, existing.Response, transactionId);
 
        if (_transactionIdByRrn.TryGetValue(rrn, out var holder) && holder != transactionId)
            return (StoreOutcome.RrnInUse, null, holder);
 
        return (null, null, transactionId);
    }
}