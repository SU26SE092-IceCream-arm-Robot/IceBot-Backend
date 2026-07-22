using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.SalesCatalog.RuntimeMenus.Results;

namespace Application.SalesCatalog.RuntimeMenus.Support;

internal static class RuntimeMenuRevision
{
    public static string Compute(Guid kioskId, IReadOnlyCollection<RuntimeMenuItemResult> items)
    {
        var canonicalPayload = JsonSerializer.Serialize(new { KioskId = kioskId, Items = items });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload))).ToLowerInvariant();
    }
}
