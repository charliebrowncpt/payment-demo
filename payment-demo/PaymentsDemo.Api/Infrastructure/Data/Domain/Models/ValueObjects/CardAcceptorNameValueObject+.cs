namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

/// The 40-character FNB descriptor, read by position. Computed, never stored.
public sealed record CardAcceptorName
{
    public const string FacilitatorPrefix = "ZAP";
    public const int NameWidth = 23;   // positions 1–23
    public const int CityWidth = 13;   // positions 24–36
    public const int RegionWidth = 2;  // positions 37–38
    public const int CountryWidth = 2; // positions 39–40
    public const int Length = NameWidth + CityWidth + RegionWidth + CountryWidth;

    public string Value { get; }
    private CardAcceptorName(string value) => Value = value;

    public static CardAcceptorName From(TradingNameValueObject tradingName, AddressValueObject address)
    {
        var name = $"{FacilitatorPrefix}*{tradingName.Value}".PadRight(NameWidth);
        var city = address.City.Length > CityWidth ? address.City[..CityWidth] : address.City;

        var value = string.Concat(
            name,
            city.PadRight(CityWidth),
            address.RegionCode,
            address.CountryCode);

        // Guards the positional layout. A failure here is a bug in the value objects.
        if (value.Length != Length)
            throw new InvalidOperationException($"CardAcceptorName must be {Length} characters, was {value.Length}.");

        return new CardAcceptorName(value);
    }
}