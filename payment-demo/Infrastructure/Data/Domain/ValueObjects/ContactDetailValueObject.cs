namespace PaymentDemo.Infrastructure.Domain.ValueObjects;

public record ContactDetailValueObject(
    Guid Id,
    string EmailAddress,
    string PhoneNumber
);