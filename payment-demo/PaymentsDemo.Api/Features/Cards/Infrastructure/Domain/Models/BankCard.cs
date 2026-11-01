using PaymentsDemo.Api.Features.Cards.Infrastructure.Domain.Enums;
using PaymentsDemo.Api.Shared.Utilities;

namespace PaymentsDemo.Api.Features.Cards.Infrastructure.Domain.Models;

public class BankCard
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }

    public string CardNumber { get; private set; } = string.Empty;
    public string Cvc { get; private set; } = string.Empty;
    public int ExpiresOnDay { get; private set; }
    public int ExpiresOnMonth { get; private set; }
    public int ExpiresOnYear { get; private set; }

    public BankCardStatus CardStatus { get; private set; }

    public string NameOnCard { get; private set; } = string.Empty;

    public DateTimeOffset CreatedOnUTC { get; private set; }
    public Guid CreatedBy { get; private set; }

    public DateTimeOffset? UpdatedOnUTC { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public string? CardToken { get; private set; }

    // Required by EF Core
    private BankCard() { }


    public BankCard(
        Guid customerId,
        string cardNumber,
        string nameOnCard,
        string cvc,
        int expiresOnDay,
        int expiresOnMonth,
        int expiresOnYear,
        BankCardStatus cardStatus,
        Guid createdBy)
    {
        Id = Guid.NewGuid();

        CustomerId = customerId;

        CardNumber = cardNumber;
        NameOnCard = nameOnCard;
        Cvc = cvc;
        ExpiresOnDay = expiresOnDay;
        ExpiresOnMonth = expiresOnMonth;
        ExpiresOnYear = expiresOnYear;
        CardStatus = cardStatus;

        CreatedBy = createdBy;
        CreatedOnUTC = DateTimeOffset.UtcNow;

        string hash = $"{Cvc}|{ExpiresOnYear}_{NameOnCard}|_^_{ExpiresOnMonth}@{CardNumber}^_^";

        CardToken = HashGenerator.GenerateHash(hash + " (_) ");
    }
}