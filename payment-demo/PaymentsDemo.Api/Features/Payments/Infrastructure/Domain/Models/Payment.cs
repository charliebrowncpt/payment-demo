using PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.Enums;
using PaymentsDemo.Api.Infrastructure.Data.Domain;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

namespace PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.Models;

public class Payment
{
    private readonly List<PaymentAdjustment> _adjustments = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid CardId { get; private set; }
    public SubMerchantCodeValueObject SubMerchantCode { get; private set; } = null!;
    public long Amount { get; private set; }

    // Identifiers. All derived from the idempotency key.
    public Guid IdempotencyKey { get; private set; }
    public Guid TransactionId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public string RetrievalReferenceNumber { get; private set; } = string.Empty;

    public string RequestHash { get; private set; } = string.Empty;
    public Guid CorrelationId { get; private set; }

    public PaymentStatus Status { get; private set; }

    // Contains the card token and email. Never log it.
    public string AcmeEnvelopeJson { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? EnqueuedAtUtc { get; private set; }

    // Filled in when Acme answers.
    public string? AcmeResponseCode { get; private set; }
    public string? ReconId { get; private set; }
    public string? AuthCode { get; private set; }
    public long? AuthAmount { get; private set; }

    public long RefundedAmount { get; private set; }

    public IReadOnlyCollection<PaymentAdjustment> Adjustments => _adjustments.AsReadOnly();

    /// What the bank actually authorised. Falls back to the requested amount.
    public long SettledAmount => AuthAmount ?? Amount;
    public long RefundableAmount => SettledAmount - RefundedAmount;

    private Payment() { } // EF Core

    /// Creates a payment in Pending. The Acme envelope is attached before the first save.
    public Payment(
        Guid customerId,
        Guid cardId,
        SubMerchantCodeValueObject subMerchantCode,
        long amount,
        Guid idempotencyKey,
        string requestHash,
        Guid correlationId,
        DateTimeOffset now)
    {
        //INVARIANT: The payment amount is always positive. Refunds and reversals are recorded as adjustments.
        if (amount <= 0) throw new DomainException("Amount must be positive.");

        ArgumentNullException.ThrowIfNull(subMerchantCode);

        // TransactionID keeps the hyphens. OrderNumber drops them. The first 12 characters become the RRN.
        var orderNumber = idempotencyKey.ToString().Replace("-", string.Empty, StringComparison.Ordinal);

        Id = Guid.NewGuid();
        CustomerId = customerId;
        CardId = cardId;
        SubMerchantCode = subMerchantCode;
        Amount = amount;
        IdempotencyKey = idempotencyKey;
        TransactionId = idempotencyKey;
        OrderNumber = orderNumber;
        RetrievalReferenceNumber = orderNumber[..12];
        RequestHash = requestHash;
        CorrelationId = correlationId;
        Status = PaymentStatus.Pending;
        CreatedAtUtc = now;
    }

    /// [INVARIANT] Sets the Acme request. Allowed once, before the payment is queued.
    public void AttachAcmeEnvelope(string json)
    {
        if (EnqueuedAtUtc is not null)
            throw new DomainException("The Acme request can't change after the payment is queued.");

        if (!string.IsNullOrEmpty(AcmeEnvelopeJson))
            throw new DomainException("The Acme request is already set.");

        if (string.IsNullOrWhiteSpace(json))
            throw new DomainException("The Acme request can't be empty.");

        AcmeEnvelopeJson = json;
    }

    /// Records that the envelope was published. Repeating it is a no-op.
    public void MarkEnqueued(DateTimeOffset now)
    {
        if (string.IsNullOrEmpty(AcmeEnvelopeJson))
            throw new DomainException("Can't queue a payment without an Acme request.");

        if (Status != PaymentStatus.Pending)
            throw new DomainException($"Can't queue a payment in status {Status}.");

        EnqueuedAtUtc ??= now;
    }

    /// Acme did not answer in time. The outcome is unknown until a retry or notification resolves it.
    public void MarkUnknown()
    {
        // A result that already arrived takes precedence over a timeout.
        if (Status != PaymentStatus.Pending)
            return;

        Status = PaymentStatus.Unknown;
    }

    /// Applies Acme's answer. Idempotent: the same answer twice changes nothing.
    public void ApplyAcmeOutcome(AcmeOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        if (outcome.Status is not (PaymentStatus.Approved or PaymentStatus.Declined or PaymentStatus.Rejected))
            throw new DomainException("An Acme outcome must be Approved, Declined or Rejected.");

        if (outcome.Status == PaymentStatus.Approved && outcome.AuthAmount is not > 0)
            throw new DomainException("An approved outcome needs a positive AuthAmount.");

        var resolvable = Status is PaymentStatus.Pending or PaymentStatus.Unknown;

        if (!resolvable)
        {
            // Duplicate delivery of the same answer. Otherwise the caller has a conflict to investigate.
            if (Status == outcome.Status)
                return;

            throw new DomainException($"Payment is already {Status}. Cannot apply {outcome.Status}.");
        }

        Status = outcome.Status;
        AcmeResponseCode = outcome.ResponseCode;
        ReconId = outcome.ReconId;
        AuthCode = outcome.AuthCode;
        AuthAmount = outcome.Status == PaymentStatus.Approved ? outcome.AuthAmount : null;
    }

    /// Full reversal of an approved payment. Duplicate events are ignored.
    public void RecordReversal(string eventId, long amount, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new DomainException("Event ID is required.");

        if (HasAdjustment(eventId))
            return;

        if (Status != PaymentStatus.Approved)
            throw new DomainException($"A reversal can't apply to a payment in status {Status}.");

        if (RefundedAmount > 0)
            throw new DomainException("A payment with refunds can't be reversed.");

        if (amount != SettledAmount)
            throw new DomainException($"Reversal must cover the full settled amount ({SettledAmount}).");

        _adjustments.Add(new PaymentAdjustment(Id, eventId, AdjustmentType.Reversal, amount, now));

        Status = PaymentStatus.Reversed;
    }

    /// Partial or full refund of an approved payment. Duplicate events are ignored.
    public void RecordRefund(string eventId, long amount, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new DomainException("Event ID is required.");

        if (HasAdjustment(eventId))
            return;

        if (Status != PaymentStatus.Approved)
            throw new DomainException($"A refund can't apply to a payment in status {Status}.");

        if (amount <= 0)
            throw new DomainException("Refund amount must be positive.");

        if (amount > RefundableAmount)
            throw new DomainException($"Refund of {amount} exceeds the refundable balance of {RefundableAmount}.");

        _adjustments.Add(new PaymentAdjustment(Id, eventId, AdjustmentType.Refund, amount, now));

        RefundedAmount += amount;

        // Partial refunds keep the payment Approved. Only a full refund changes the status.
        if (RefundableAmount == 0)
            Status = PaymentStatus.Refunded;
    }

    private bool HasAdjustment(string eventId) => _adjustments.Any(a => a.EventId == eventId);
}

/// The result of an Acme call, translated into domain terms by the caller.
public sealed record AcmeOutcome(
    PaymentStatus Status,
    string ResponseCode,
    string? ReconId,
    string? AuthCode,
    long? AuthAmount
);