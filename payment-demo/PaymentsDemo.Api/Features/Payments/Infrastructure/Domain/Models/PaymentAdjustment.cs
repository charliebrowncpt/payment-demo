using PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.Enums;

namespace PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.Models;

public class PaymentAdjustment
{
    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public string EventId { get; private set; } = string.Empty;   // Acme Switch transactionId
    public AdjustmentType Type { get; private set; }
    public long Amount { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }

    private PaymentAdjustment() { } // EF Core

    // internal: adjustments are only created through the Paymetn aggregate root.
    internal PaymentAdjustment(Guid paymentId, string eventId, AdjustmentType type, long amount, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        PaymentId = paymentId;
        EventId = eventId;
        Type = type;
        Amount = amount;
        ReceivedAtUtc = now;
    }
}