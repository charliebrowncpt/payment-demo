namespace PaymentDemo.Infrastructure.Domain;

public class SubMerchant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedUTC { get; private set; }
    public Guid CreatedBy { get; private set; }

    public DateTimeOffset? UpdatedUTC { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public bool IsActive { get; private set; }

    // Required by EF Core
    private SubMerchant() { }

    public SubMerchant(string name, Guid createdBy)
    {
       
        Id = Guid.NewGuid();
        Name = name.Trim();
        CreatedUTC = DateTimeOffset.UtcNow;
        CreatedBy = createdBy;
        IsActive = true;
    }
}