using System.Globalization;

namespace AcmePay.Shared;

// Timestamps are ISO 8601 without an offset, in South African Standard Time (UTC+02:00,
// no DST), to millisecond precision: 2026-03-01T13:56:36.533
public static class Sast
{
    // Kind is Unspecified so System.Text.Json writes no "Z" or offset.
    public static DateTime Now() => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(2), DateTimeKind.Unspecified);

    public static string Stamp() => Now().ToString("yyyy-MM-dd'T'HH:mm:ss.fff", CultureInfo.InvariantCulture);
}