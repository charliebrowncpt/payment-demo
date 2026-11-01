using System.Diagnostics.CodeAnalysis;

namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

/// Acme SubMerchantID: 1–15 ASCII letters or digits.
public sealed record SubMerchantCodeValueObject
{
    public const int MaxLength = 15;

    public string Value { get; }
    private SubMerchantCodeValueObject(string value) => Value = value;

    public static SubMerchantCodeValueObject Create(Guid? raw) =>
        TryCreate(raw, out var code)
            ? code
            : throw new DomainException("Sub-merchant code must be 1-15 letters or digits.");

    public static bool TryCreate(Guid? raw, [NotNullWhen(true)] out SubMerchantCodeValueObject? code)
    {
        var value = raw?.ToString()?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength || !value.All(char.IsAsciiLetterOrDigit))
        {
            code = null;
            return false;
        }

        code = new SubMerchantCodeValueObject(value);
        return true;
    }

    public override string ToString() => Value;
}