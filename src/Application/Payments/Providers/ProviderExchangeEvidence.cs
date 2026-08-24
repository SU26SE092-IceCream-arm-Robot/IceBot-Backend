using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text;

namespace Application.Payments.Providers;

public sealed record ProviderExchangeEvidence(
    string? RequestPayloadJson,
    string? ResponsePayloadJson,
    int? HttpStatusCode)
{
    public const int MaximumPayloadLength = 256 * 1024;

    public static ProviderExchangeEvidence FromRaw(string? requestPayloadJson, string? responsePayloadJson, int? httpStatusCode) =>
        new(RedactAndBound(requestPayloadJson), RedactAndBound(responsePayloadJson), httpStatusCode);

    private static string? RedactAndBound(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;
        if (Encoding.UTF8.GetByteCount(payload) > MaximumPayloadLength)
        {
            return BuildMetadataEnvelope("truncated", payload);
        }

        JsonNode? root;
        try { root = JsonNode.Parse(payload); }
        catch (JsonException) { return BuildMetadataEnvelope("unparseable", payload); }

        Redact(root);
        var sanitized = root?.ToJsonString();
        return string.IsNullOrWhiteSpace(sanitized)
            ? null
            : Encoding.UTF8.GetByteCount(sanitized) <= MaximumPayloadLength
                ? sanitized
                : BuildMetadataEnvelope("truncated", sanitized);
    }

    private static string BuildMetadataEnvelope(string state, string payload) =>
        new JsonObject
        {
            [state] = true,
            ["byteLength"] = Encoding.UTF8.GetByteCount(payload),
            ["sha256"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant()
        }.ToJsonString();

    private static void Redact(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var entry in obj.ToList())
            {
                if (IsSensitive(entry.Key)) obj[entry.Key] = "[REDACTED]";
                else Redact(entry.Value);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array) Redact(item);
        }
    }

    private static bool IsSensitive(string name)
    {
        var normalized = string.Concat(name.Where(char.IsLetterOrDigit)).ToUpperInvariant();
        return normalized.Contains("SIGNATURE", StringComparison.Ordinal) ||
               normalized.Contains("SECRET", StringComparison.Ordinal) ||
               normalized.Contains("TOKEN", StringComparison.Ordinal) ||
               normalized.Contains("AUTHORIZATION", StringComparison.Ordinal) ||
               normalized.Contains("APIKEY", StringComparison.Ordinal) ||
               normalized.Contains("CHECKSUM", StringComparison.Ordinal) ||
               normalized.Contains("ACCOUNTNUMBER", StringComparison.Ordinal) ||
               normalized.Contains("ACCOUNTNAME", StringComparison.Ordinal) ||
               normalized.Contains("QRCODE", StringComparison.Ordinal) ||
               normalized.Contains("BUYERNAME", StringComparison.Ordinal) ||
               normalized.Contains("BUYEREMAIL", StringComparison.Ordinal) ||
               normalized.Contains("BUYERPHONE", StringComparison.Ordinal) ||
               normalized.Contains("BUYERADDRESS", StringComparison.Ordinal);
    }
}
