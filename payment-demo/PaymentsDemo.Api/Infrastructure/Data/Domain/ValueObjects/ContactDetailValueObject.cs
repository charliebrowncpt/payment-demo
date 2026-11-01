namespace PaymentsDemo.Api.Infrastructure.Data.Domain.ValueObjects;

public record ContactDetailValueObject(
    Guid Id,
    string EmailAddress,
    string PhoneNumber
);