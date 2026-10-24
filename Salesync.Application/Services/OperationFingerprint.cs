using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Salesync.Application.Services;

public static class OperationFingerprint
{
    public static string Compute(string operation, string json)
    {
        using var document = JsonDocument.Parse(json);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) Write(writer, document.RootElement);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(operation + "\n" + Encoding.UTF8.GetString(stream.ToArray()))));
    }

    private static void Write(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                { writer.WritePropertyName(property.Name); Write(writer, property.Value); }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var element in value.EnumerateArray()) Write(writer, element);
                writer.WriteEndArray();
                break;
            case JsonValueKind.Number when value.TryGetDecimal(out var number):
                writer.WriteRawValue(number.ToString("G29", CultureInfo.InvariantCulture));
                break;
            default: value.WriteTo(writer); break;
        }
    }
}
