namespace PaymentsDemo.Api.Infrastructure.Data.Domain;

public class Merchant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedUTC { get; private set; }
    public Guid CreatedBy { get; private set; }

    public DateTimeOffset? UpdatedUTC { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public bool IsActive { get; private set; }

 // Required by EF Core
    private Merchant() { }


    public Merchant(
        string name,
        bool isActive,
        Guid createdBy)
    {
        Name = name;
        IsActive = isActive;

        CreatedBy = createdBy;
        CreatedUTC = DateTimeOffset.UtcNow;
    }
}