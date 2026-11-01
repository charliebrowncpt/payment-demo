namespace PaymentsDemo.Api.Features.Payments.Infrastructure.Domain.Enums;
public enum PaymentStatus
{
    Pending,    // submitted, not yet answered by Acme
    Approved,
    Declined,   // Acme answered 00 is not returned, acquirer declined
    Rejected,   // Acme refused the request (400 or 409)
    Unknown,    // timed out, outcome not known
    Reversed,   // approved, then reversed in full
    Refunded    // approved, then fully refunded
}