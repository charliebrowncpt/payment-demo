using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

/// Acme SubMerchantID: 1–15 ASCII letters or digits.
public sealed record SubMerchantCodeValueObject
{
    public const int MaxLength = 15;

    public Guid Value { get; }
    private SubMerchantCodeValueObject(Guid value) => Value = value;

    public static SubMerchantCodeValueObject Create(Guid? raw) =>
        TryCreate(raw, out var code)
            ? code
            : throw new DomainException("Sub-merchant code must be 1-15 letters or digits.");

    public static bool TryCreate(Guid? raw, [NotNullWhen(true)] out SubMerchantCodeValueObject? code)
    {
        if (raw == null)
        {
            code = null;
            return false;
        }

        code = new SubMerchantCodeValueObject(raw.Value);
        return true;
    }
}

public sealed class SubMerchantCodeConverter() : ValueConverter<SubMerchantCodeValueObject, Guid>(
    v => v.Value,
    v => SubMerchantCodeValueObject.Create(v)
);