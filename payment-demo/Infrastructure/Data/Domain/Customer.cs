namespace PaymentDemo.Infrastructure.Domain;

using PaymentDemo.Infrastructure.Domain.ValueObjects;

public class Customer
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public ContactDetailValueObject ContactInformation { get; private set; }
    public DateTimeOffset ExpiryDate { get; private set; }

    public bool IsActive { get; private set; }

    public string NameOnCard { get; private set; } = string.Empty;

    public DateTimeOffset CreatedUTC { get; private set; }
    public Guid CreatedBy { get; private set; }

    public DateTimeOffset? UpdatedUTC { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public string? CardToken { get; private set; }

    // Required by EF Core
    private Customer() { }

    public Customer(
        string name,
        ContactDetailValueObject contactInformation,
        bool isActive,
        Guid createdBy)
    {
        Id = Guid.NewGuid();

        Name = name;
        ContactInformation = contactInformation;

        IsActive = isActive;

        CreatedBy = createdBy;
        CreatedUTC = DateTimeOffset.UtcNow;
    }
}