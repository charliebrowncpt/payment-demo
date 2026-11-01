using PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.SubMerchants;

public class SubMerchant
{
    public Guid Id { get; private set; }
    public Guid MerchantId { get; private set; }

    public SubMerchantCodeValueObject Code { get; private set; } = null!;           // Acme SubMerchantID
    public TradingNameValueObject TradingName { get; private set; } = null!;
    public AddressValueObject Address { get; private set; } = null!;
    public MerchantCategoryCodeValueObject MerchantCategoryCode { get; private set; } = null!;
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    private SubMerchant() { } // EF Core

    // internal: sub-merchants are only created through Merchant.RegisterSubMerchant.
    internal SubMerchant(
        Guid merchantId,
        SubMerchantCodeValueObject code,
        TradingNameValueObject tradingName,
        AddressValueObject address,
        MerchantCategoryCodeValueObject mcc,
        Guid createdBy)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(tradingName);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(mcc);

        Id = Guid.NewGuid();
        MerchantId = merchantId;
        Code = code;
        TradingName = tradingName;
        Address = address;
        MerchantCategoryCode = mcc;
        IsActive = true;
        CreatedBy = createdBy;
        CreatedOnUtc = DateTimeOffset.UtcNow;
    }

    internal void Rename(TradingNameValueObject name, Guid by)
    {
        TradingName = name;
        Touch(by);
    }

    internal void SetActive(bool active, Guid by)
    {
        IsActive = active;
        Touch(by);
    }

    /// Used by the envelope builder. Derived, so it is not persisted.
    public CardAcceptorName BuildCardAcceptorName() => CardAcceptorName.From(TradingName, Address);

    private void Touch(Guid by)
    {
        UpdatedBy = by;
        UpdatedOnUtc = DateTimeOffset.UtcNow;
    }
}