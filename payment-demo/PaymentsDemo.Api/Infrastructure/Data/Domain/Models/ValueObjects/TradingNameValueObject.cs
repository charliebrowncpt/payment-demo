using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

/// Shown on the cardholder's statement. Max 19 characters, because FNB positions 1–23
/// hold "ZAP*" plus the name.
public sealed record TradingNameValueObject
{
    public static readonly int MaxLength = CardAcceptorName.NameWidth - (CardAcceptorName.FacilitatorPrefix.Length + 1);

    public string Value { get; }
    private TradingNameValueObject(string value) => Value = value;

    public static TradingNameValueObject Create(string? raw)
    {
        var value = raw?.Trim().ToUpperInvariant() ?? string.Empty;

        if (value.Length is 0 || value.Length > MaxLength)
            throw new DomainException($"Trading name must be 1-{MaxLength} characters.");

        if (!value.All(c => char.IsAsciiLetterOrDigit(c) || c is ' ' or '&' or '-' or '.'))
            throw new DomainException("Trading name contains characters the card scheme doesn't accept.");

        return new TradingNameValueObject(value);
    }
}

public sealed class TradingAsConverter() : ValueConverter<TradingNameValueObject, string>(
    v => v.Value,
    v => TradingNameValueObject.Create(v)
);