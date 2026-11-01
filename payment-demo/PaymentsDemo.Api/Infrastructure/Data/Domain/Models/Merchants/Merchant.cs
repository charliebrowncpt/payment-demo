using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.SubMerchants;
using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.Merchants;

public enum MerchantStatus { Active, Suspended, Closed }

public class Merchant
{
    private readonly List<SubMerchant> _subMerchants = [];

    public Guid Id { get; private set; }
    public string LegalName { get; private set; } = string.Empty;
    public MerchantStatus Status { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public IReadOnlyCollection<SubMerchant> SubMerchants => _subMerchants.AsReadOnly();

    private Merchant() { } // EF Core

    public Merchant(string legalName, Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(legalName))
            throw new DomainException("Legal name is required.");

        Id = Guid.NewGuid();
        LegalName = legalName.Trim();
        Status = MerchantStatus.Active;
        CreatedBy = createdBy;
        CreatedOnUtc = DateTimeOffset.UtcNow;
    }

    public SubMerchant RegisterSubMerchant(
        SubMerchantCodeValueObject code,
        TradingNameValueObject tradingName,
        AddressValueObject address,
        MerchantCategoryCodeValueObject mcc,
        Guid by)
    {
        EnsureNotClosed();

        // Uniqueness across all merchants is enforced by the database alternate key.
        // This check gives a clear error for the common case.
        if (_subMerchants.Any(s => s.Code == code))
            throw new DomainException($"Sub-merchant {code} is already registered.");

        var sub = new SubMerchant(Id, code, tradingName, address, mcc, by);
        _subMerchants.Add(sub);
        Touch(by);
        return sub;
    }

    public void ChangeTradingName(SubMerchantCodeValueObject code, TradingNameValueObject name, Guid by)
    {
        EnsureNotClosed();
        Find(code).Rename(name, by);
        Touch(by);
    }

    public void SetSubMerchantActive(SubMerchantCodeValueObject code, bool active, Guid by)
    {
        Find(code).SetActive(active, by);
        Touch(by);
    }

    public void Suspend(Guid by)
    {
        if (Status != MerchantStatus.Active)
            throw new DomainException("Only an active merchant can be suspended.");

        Status = MerchantStatus.Suspended;
        Touch(by);
    }

    public void Reinstate(Guid by)
    {
        if (Status != MerchantStatus.Suspended)
            throw new DomainException("Only a suspended merchant can be reinstated.");

        Status = MerchantStatus.Active;
        Touch(by);
    }

    public void Close(Guid by)
    {
        if (Status == MerchantStatus.Closed)
            return; // idempotent

        Status = MerchantStatus.Closed;
        Touch(by);
    }

    /// The single place that decides whether a payment may be taken for a sub-merchant.
    public bool CanAcceptPaymentsFor(SubMerchantCodeValueObject code) =>
        Status == MerchantStatus.Active &&
        _subMerchants.Any(s => s.Code == code && s.IsActive);

    private SubMerchant Find(SubMerchantCodeValueObject code) =>
        _subMerchants.FirstOrDefault(s => s.Code == code)
        ?? throw new DomainException($"Sub-merchant {code} not found.");

    private void EnsureNotClosed()
    {
        if (Status == MerchantStatus.Closed)
            throw new DomainException("A closed merchant can't change its sub-merchants.");
    }

    private void Touch(Guid by)
    {
        UpdatedBy = by;
        UpdatedOnUtc = DateTimeOffset.UtcNow;
    }
}