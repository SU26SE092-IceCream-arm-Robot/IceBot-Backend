using System.Text.Json;
using System.Text.Json.Nodes;

namespace Application.Utils
{
    public static class SensitiveDataMasker
    {
        private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "pass", "pwd",
            "token", "jwtToken", "accessToken", "refreshToken",
            "jwt", "secret", "authorization", "authToken"
        };

        public static string MaskSensitiveData(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;

            try
            {
                var root = JsonNode.Parse(json);
                MaskNode(root);
                return root?.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }) ?? json;
            }
            catch
            {
                return json;
            }
        }

        private static void MaskNode(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                foreach (var kvp in obj.ToList())
                {
                    if (SensitiveKeys.Contains(kvp.Key))
                    {
                        obj[kvp.Key] = "[REDACTED]";
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
    }
}
