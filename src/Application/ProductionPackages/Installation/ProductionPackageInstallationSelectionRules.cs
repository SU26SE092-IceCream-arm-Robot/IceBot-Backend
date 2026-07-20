using Domain.ProductionPackages;
using Domain.Common;

namespace Application.ProductionPackages.Installation;

public sealed record ProductionPackageInstallationSelection(
    IReadOnlyCollection<ProductionPackageRouteBlueprint> Routes,
    IReadOnlyCollection<ProductionPackageProgramBlueprint> Programs,
    IReadOnlyCollection<ProductionPackageArtifactDefinition> Artifacts);

public static class ProductionPackageInstallationSelectionRules
{
    public static ProductionPackageInstallationSelection Resolve(
        ProductionPackageVersion version,
        IReadOnlySet<string> selectedProductKeys)
    {
        var routes = version.Routes
            .Where(route => selectedProductKeys.Contains(route.ProductSourceKey))
            .ToArray();
        var routedProductKeys = routes.Select(route => route.ProductSourceKey)
            .ToHashSet(StringComparer.Ordinal);
        var missingRouteKeys = selectedProductKeys.Where(key => !routedProductKeys.Contains(key)).ToArray();
        if (missingRouteKeys.Length > 0)
            throw new DomainRuleException(
                $"Selected package products require execution routes: {string.Join(", ", missingRouteKeys)}.");
        var blueprintCodes = routes.Select(route => route.ProgramBlueprintCode)
            .ToHashSet(StringComparer.Ordinal);
        var programs = version.Programs
            .Where(program => blueprintCodes.Contains(program.BlueprintCode))
            .ToArray();
        var artifactSourceKeys = programs.SelectMany(program => program.Slots)
            .Select(slot => slot.ArtifactSourceKey)
            .ToHashSet(StringComparer.Ordinal);
        var artifacts = version.Artifacts
            .Where(artifact => artifactSourceKeys.Contains(artifact.SourceKey))
            .ToArray();
        return new ProductionPackageInstallationSelection(routes, programs, artifacts);
    }
}
