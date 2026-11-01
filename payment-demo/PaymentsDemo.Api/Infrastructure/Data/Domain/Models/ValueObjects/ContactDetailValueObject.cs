namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

public record ContactDetailValueObject(
    Guid Id,
    string EmailAddress,
    string PhoneNumber
);