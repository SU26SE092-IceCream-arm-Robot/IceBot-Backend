using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Common;
using Domain.ProductionPackages;

namespace Application.ProductionPackages.Installation;

public static class ProductionPackageInstallationRequestRules
{
    public static IReadOnlySet<string> ResolveSelectedProductKeys(
        ProductionPackageVersion version,
        IReadOnlyCollection<string> requestedProductKeys)
    {
        var selected = requestedProductKeys.Count == 0
            ? version.Products.Select(product => product.SourceKey).ToHashSet(StringComparer.Ordinal)
            : requestedProductKeys.Select(NormalizeProductKey).ToHashSet(StringComparer.Ordinal);
        if (selected.Count == 0)
            throw new DomainRuleException("A production package installation requires at least one product.");
        var available = version.Products.Select(product => product.SourceKey).ToHashSet(StringComparer.Ordinal);
        var unknown = selected.Where(key => !available.Contains(key)).Order(StringComparer.Ordinal).ToArray();
        if (unknown.Length > 0)
            throw new DomainRuleException(
                $"Selected package products do not exist: {string.Join(", ", unknown)}.");
        return selected;
    }

    public static string ComputeRequestChecksum(
        Guid organizationId,
        Guid? storeId,
        Guid? kioskId,
        ProductionPackageVersion version,
        IReadOnlyCollection<string> selectedProductKeys,
        string? materializationIdentitySuffix = null)
    {
        var payload = JsonSerializer.Serialize(new
        {
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            PackageVersionId = version.Id,
            version.ManifestChecksum,
            MaterializationIdentitySuffix = string.IsNullOrWhiteSpace(materializationIdentitySuffix)
                ? null
                : materializationIdentitySuffix.Trim().ToUpperInvariant(),
            ProductSourceKeys = selectedProductKeys.OrderBy(key => key, StringComparer.Ordinal)
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static string NormalizeProductKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainRuleException("Selected package product key is required.");
        return value.Trim().ToUpperInvariant();
    }
}
