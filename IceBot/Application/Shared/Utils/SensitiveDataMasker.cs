using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Application.Shared.Utils
{
    public static class SensitiveDataMasker
    {
        private const string RedactedValue = "[REDACTED]";

        private static readonly HashSet<string> ExactSensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "apiKey",
            "authorization",
            "authToken",
            "clientSecret",
            "confirmPassword",
            "currentPassword",
            "externalIdToken",
            "firebaseToken",
            "idToken",
            "initialPassword",
            "jwt",
            "jwtToken",
            "newPassword",
            "pass",
            "password",
            "passwordHash",
            "providerSignature",
            "pwd",
            "refreshToken",
            "resetToken",
            "secret",
            "secretKey",
            "signature",
            "token",
            "tokenHash"
        };

        private static readonly string[] SensitiveKeyFragments =
        [
            "apiKey",
            "authorization",
            "credential",
            "password",
            "secret",
            "signature",
            "token"
        ];

        private static readonly HashSet<string> SensitivePayloadKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "checkoutUrl",
            "headersJson",
            "payloadJson",
            "providerPayload",
            "qrCodePayload",
            "rawRequestJson",
            "rawResponseJson"
        };

        private static readonly Regex JsonStringValueRegex = new(
            "(?<prefix>\"(?<key>[^\"]+)\"\\s*:\\s*)\"(?<value>(?:\\\\.|[^\"\\\\])*)\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex FormValueRegex = new(
            @"(?<prefix>(?<key>[A-Za-z0-9_.-]*(?:password|token|secret|signature|authorization|apiKey|credential)[A-Za-z0-9_.-]*)=)(?<value>[^&\s]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static string MaskSensitiveData(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            try
            {
                var root = JsonNode.Parse(value);
                MaskNode(root);
                return root?.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? value;
            }
            catch
            {
                return MaskText(value);
            }
        }

        private static void MaskNode(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                foreach (var kvp in obj.ToList())
                {
                    if (IsSensitiveKey(kvp.Key))
                    {
                        obj[kvp.Key] = RedactedValue;
                    }
                    else
                    {
                        MaskNode(obj[kvp.Key]);
                    }
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    MaskNode(item);
                }
            }
        }

        private static string MaskText(string value)
        {
            var masked = JsonStringValueRegex.Replace(value, match =>
            {
                var key = match.Groups["key"].Value;
                return IsSensitiveKey(key)
                    ? $"{match.Groups["prefix"].Value}\"{RedactedValue}\""
                    : match.Value;
            });

            return FormValueRegex.Replace(masked, match =>
            {
                var key = match.Groups["key"].Value;
                return IsSensitiveKey(key)
                    ? $"{match.Groups["prefix"].Value}{RedactedValue}"
                    : match.Value;
            });
        }

        private static bool IsSensitiveKey(string key)
        {
            if (ExactSensitiveKeys.Contains(key) || SensitivePayloadKeys.Contains(key))
            {
                return true;
            }

            return SensitiveKeyFragments.Any(fragment =>
                key.Contains(fragment, StringComparison.OrdinalIgnoreCase));
        }
    }
}
