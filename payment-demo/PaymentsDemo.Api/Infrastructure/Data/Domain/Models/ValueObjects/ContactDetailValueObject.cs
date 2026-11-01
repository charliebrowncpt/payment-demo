namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

public record ContactDetailValueObject(string EmailAddress, string? PhoneNumber)
{
    // EF Core needs this for owned types (AGENTS.md section 6)
    private ContactDetailValueObject() : this(string.Empty, null) { }
}