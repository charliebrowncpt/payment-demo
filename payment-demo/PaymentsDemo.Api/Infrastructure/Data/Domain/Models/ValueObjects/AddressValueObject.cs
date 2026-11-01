namespace PaymentsDemo.Api.Infrastructure.Data.Domain.Models.ValueObjects;

/// Business address. City is stored in full. The descriptor truncates it to 13.
public sealed record AddressValueObject
{
    public const int CityMaxLength = 50;

    public string City { get; private init; } = string.Empty;
    public string RegionCode { get; private init; } = string.Empty;   // e.g. WC
    public string CountryCode { get; private init; } = string.Empty;  // ISO 3166-1 alpha-2, e.g. ZA

    public AddressValueObject(string city, string regionCode, string countryCode)
    {
        var c = city?.Trim().ToUpperInvariant() ?? string.Empty;
        var r = regionCode?.Trim().ToUpperInvariant() ?? string.Empty;
        var k = countryCode?.Trim().ToUpperInvariant() ?? string.Empty;

        if (c.Length is 0 || c.Length > CityMaxLength)
            throw new DomainException($"City must be 1-{CityMaxLength} characters.");
            
        if (r.Length != 2 || !r.All(char.IsAsciiLetter))
            throw new DomainException("Region code must be 2 letters.");

        if (k.Length != 2 || !k.All(char.IsAsciiLetter))
            throw new DomainException("Country code must be 2 letters.");

        City = c;
        RegionCode = r;
        CountryCode = k;
    }
}
