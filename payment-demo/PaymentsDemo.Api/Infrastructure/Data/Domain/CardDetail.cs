namespace PaymentsDemo.Api.Infrastructure.Data.Domain;

public class CardDetail
{
    public Guid Id { get; private set; }

    public string CardNumber { get; private set; } = string.Empty;
    public string Cvc { get; private set; } = string.Empty;
    public int ExpiresOnDay { get; private set; }
    public int ExpiresOnMonth { get; private set; }
    public int ExpiresOnYear { get; private set; }

    public bool IsActive { get; private set; }

    public string NameOnCard { get; private set; } = string.Empty;

    public DateTimeOffset CreatedUTC { get; private set; }
    public Guid CreatedBy { get; private set; }

    public DateTimeOffset? UpdatedUTC { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public string? CardToken { get; private set; }

 // Required by EF Core
    private CardDetail() { }


    public CardDetail(
        string cardNumber,
        string nameOnCard,
        string cvc,
        int expiresOnDay,
        int expiresOnMonth,
        int expiresOnYear,
        bool isActive,
        Guid createdBy)
    {
        Id = Guid.NewGuid();

        CardNumber = cardNumber;
        NameOnCard = nameOnCard;
        Cvc = cvc;
        ExpiresOnDay = expiresOnDay;
        ExpiresOnMonth = expiresOnMonth;
        ExpiresOnYear = expiresOnYear;
        IsActive = isActive;

        CreatedBy = createdBy;
        CreatedUTC = DateTimeOffset.UtcNow;
    }
}