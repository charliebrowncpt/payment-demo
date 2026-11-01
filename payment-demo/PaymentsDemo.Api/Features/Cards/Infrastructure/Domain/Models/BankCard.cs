using PaymentsDemo.Api.Features.Cards.Infrastructure.Domain.Enums;
using PaymentsDemo.Api.Shared.Utilities;

namespace PaymentsDemo.Api.Features.Cards.Infrastructure.Domain.Models;

public class BankCard
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }

    public string CardNumber { get; private set; } = string.Empty;
    public string Cvc { get; private set; } = string.Empty;
    public int ExpiryDay { get; private set; }
    public int ExpiryMonth { get; private set; }
    public int ExpiryYear { get; private set; }

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
        int ExpiryDay,
        int ExpiryMonth,
        int ExpiryYear,
        BankCardStatus cardStatus,
        Guid createdBy)
    {
        Id = Guid.NewGuid();

        CustomerId = customerId;

        CardNumber = cardNumber;
        NameOnCard = nameOnCard;
        Cvc = cvc;
        ExpiryDay = ExpiryDay;
        ExpiryMonth = ExpiryMonth;
        ExpiryYear = ExpiryYear;
        CardStatus = cardStatus;

        CreatedBy = createdBy;
        CreatedOnUTC = DateTimeOffset.UtcNow;

        string hash = $"{Cvc}|{ExpiryYear}_{NameOnCard}|_^_{ExpiryMonth}@{CardNumber}^_^";

        CardToken = HashGenerator.GenerateHash(hash + " (_) ");
    }
}