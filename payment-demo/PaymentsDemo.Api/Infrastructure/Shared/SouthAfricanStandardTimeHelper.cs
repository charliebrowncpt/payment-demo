using System.Globalization;

namespace PaymentsDemo.Api.Infrastructure.Shared;

public static class SouthAfricanStandardTimeHelper
{
    // Kind is Unspecified so System.Text.Json writes no "Z" or offset.
    public static DateTime Now() => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(2), DateTimeKind.Unspecified);

    public static string Stamp() => Now().ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture);
}