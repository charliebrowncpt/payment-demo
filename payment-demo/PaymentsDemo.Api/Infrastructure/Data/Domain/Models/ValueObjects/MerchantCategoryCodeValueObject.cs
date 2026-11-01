using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

/// Acme MerchantCategoryCode: exactly four digits.
public sealed record MerchantCategoryCodeValueObject
{
    public string Value { get; }
    private MerchantCategoryCodeValueObject(string value) => Value = value;

    public static MerchantCategoryCodeValueObject Create(string? raw)
    {
        if (raw is not { Length: 4 } || !raw.All(char.IsAsciiDigit))
            throw new DomainException("Merchant category code must be exactly 4 digits.");

        return new MerchantCategoryCodeValueObject(raw);
    }
}

public sealed class MerchantCategoryCodeConverter() : ValueConverter<MerchantCategoryCodeValueObject, string>(
    v => v.Value,
    v => MerchantCategoryCodeValueObject.Create(v)
);
