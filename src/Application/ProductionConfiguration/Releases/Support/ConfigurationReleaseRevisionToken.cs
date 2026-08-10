using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.ProductionConfiguration.Entities;

namespace Application.ProductionConfiguration.Releases.Support;

public static class ConfigurationReleaseRevisionToken
{
    public static string Create(ConfigurationRelease release)
    {
        var document = new
        {
            release.Id,
            release.OrganizationId,
            release.ReleaseNumber,
            Status = release.Status.ToString(),
            release.ReleaseManifestSchemaVersion,
            release.ReleaseChecksum,
            Routes = release.ExecutionRoutes
                .OrderBy(route => route.RouteCode, StringComparer.Ordinal)
                .ThenBy(route => route.Id)
                .Select(route => new
                {
                    route.Id,
                    route.ProductVariantId,
                    route.RecipeId,
                    route.RouteCode,
                    route.Priority,
                    route.RequiredCapabilitiesJson,
                    SupportedOptionCodes = route.GetSupportedOptionCodes().Order(StringComparer.Ordinal),
                    Bindings = route.RobotBindings
                        .OrderBy(binding => binding.BindingOrder)
                        .ThenBy(binding => binding.Id)
                        .Select(binding => new
                        {
                            binding.Id,
                            binding.ProductionProgramBindingId,
                            binding.ProductionProgramBindingChecksum,
                            binding.RobotProgramId,
                            binding.BindingOrder,
                            RequiredCapabilityCodes = binding.GetRequiredCapabilityCodes()
                        })
                })
        };
        var json = JsonSerializer.Serialize(document);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
    }

    public static bool Matches(ConfigurationRelease release, string? expectedRevision)
    {
        return !string.IsNullOrWhiteSpace(expectedRevision) &&
            string.Equals(Create(release), expectedRevision.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
