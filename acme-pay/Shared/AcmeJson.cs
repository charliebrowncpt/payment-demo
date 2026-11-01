using System.Text.Json;
using System.Text.Json.Serialization;

namespace AcmePay.Shared;

public static class AcmeJson
{
    // PascalCase, with unset optionals omitted. Used where we write JSON outside FastEndpoints.
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}