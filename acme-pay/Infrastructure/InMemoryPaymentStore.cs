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

    // Replayed: the same TransactionID within 24 hours gets the stored outcome again.
    // RrnInUse: a different TransactionID already holds this RRN (Response is null; TransactionId is the holder).
    public (StoreOutcome Outcome, PaymentResponse? Response, string TransactionId) Submit(
        string transactionId,
        string rrn, 
        Func<PaymentResponse> create)
    {
        lock (_gate)
        {
            if (_byTransactionId.TryGetValue(transactionId, out var existing) &&
                DateTime.UtcNow - existing.CreatedUtc < ReplayWindow)
                return (StoreOutcome.Replayed, existing.Response, transactionId);

            if (_transactionIdByRrn.TryGetValue(rrn, out var holder) && holder != transactionId)
                return (StoreOutcome.RrnInUse, null, holder);

            var response = create();
            _byTransactionId[transactionId] = new Entry(response, DateTime.UtcNow);
            _transactionIdByRrn[rrn] = transactionId;
            return (StoreOutcome.Created, response, transactionId);
        }
    }
}