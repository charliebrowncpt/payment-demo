using PaymentsDemo.Api.Features.Cards.Infrastructure.Domain.Enums;
using PaymentsDemo.Api.Features.Cards.Infrastructure.Domain.Models;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.Customers;

public class Customer
{
    private readonly List<BankCard> _cards = [];

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ContactDetailValueObject ContactInformation { get; private set; } = null!;
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public IReadOnlyCollection<BankCard> Cards => _cards.AsReadOnly();

    private Customer() { } // EF Core

    public Customer(string name, ContactDetailValueObject contactInformation, Guid createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(contactInformation);

        Id = Guid.NewGuid();
        Name = name;
        ContactInformation = contactInformation;
        IsActive = true;
        CreatedBy = createdBy;
        CreatedOnUtc = DateTimeOffset.UtcNow;
    }

    public BankCard AddCard(string token, string nameOnCard, string cardNumber, string cvc, int expiryMonth, int expiryYear, Guid createdBy)
    {
        if (!IsActive)
            throw new InvalidOperationException("Inactive customers can't add cards.");

        //[INVARIANT] => Fast in-memory check. The DB index on token makes this record unique in the DB
        if (_cards.Any(c => c.CardToken == token))
            throw new InvalidOperationException("This card is already stored for the customer.");

        var card = new BankCard(
            customerId: Id,
            cardNumber: cardNumber,
            nameOnCard: nameOnCard,
            cvc: cvc,
            ExpiryDay: 1,
            ExpiryMonth: expiryMonth,
            ExpiryYear: expiryYear,
            cardStatus: BankCardStatus.Active,
            createdBy: createdBy
        );
        
        _cards.Add(card);
        return card;
    }

    public void Deactivate(Guid by)
    {
        IsActive = false;
        UpdatedBy = by;
        UpdatedOnUtc = DateTimeOffset.UtcNow;
    }
}