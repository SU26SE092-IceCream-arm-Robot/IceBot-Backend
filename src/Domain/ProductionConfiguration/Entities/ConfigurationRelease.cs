using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs;
using Domain.Devices.ExecutionEndpoints;
using Domain.Common;
using Domain.ProductionConfiguration.Manifests;
using Domain.ProductionConfiguration.Enums;
using Domain.Tenants.Entities;
using Domain.ProductionConfiguration.ValueObjects;
using Domain.RobotConfiguration.Programs.Manifests;

namespace Domain.ProductionConfiguration.Entities;

public class ConfigurationRelease : BusinessEntity
{
    private readonly List<ExecutionRoute> _executionRoutes = [];

    public Guid OrganizationId { get; private set; }

    public long ReleaseNumber { get; private set; }

    public ConfigurationReleaseStatus Status { get; private set; } = ConfigurationReleaseStatus.Draft;

    public int ReleaseManifestSchemaVersion { get; private set; } = 1;

    public string? ManifestJson { get; private set; }

    public string? ReleaseChecksum { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public Guid? PublishedByAccountId { get; private set; }

    public DateTimeOffset? RetiredAt { get; private set; }

    public IReadOnlyCollection<ExecutionRoute> ExecutionRoutes => _executionRoutes;

    public virtual Organization Organization { get; private set; } = null!;

    private ConfigurationRelease()
    {
    }

    public static ConfigurationRelease CreateDraft(
        Guid organizationId,
        long releaseNumber,
        int releaseManifestSchemaVersion = 1)
    {
        if (organizationId == Guid.Empty)
        {
            throw new DomainRuleException("Configuration release organization id is required.");
        }

        if (releaseNumber <= 0)
        {
            throw new DomainRuleException("Configuration release number must be greater than zero.");
        }

        if (releaseManifestSchemaVersion <= 0)
        {
            throw new DomainRuleException("Configuration release manifest schema version must be greater than zero.");
        }

        return new ConfigurationRelease
        {
            OrganizationId = organizationId,
            ReleaseNumber = releaseNumber,
            ReleaseManifestSchemaVersion = releaseManifestSchemaVersion
        };
    }

    public ExecutionRoute AddRoute(
        Guid productVariantId,
        Guid recipeId,
        string routeCode,
        int priority,
        string? requiredCapabilitiesJson,
        IReadOnlyCollection<string> supportedOptionCodes)
    {
        EnsureDraft();

        if (_executionRoutes.Any(route => string.Equals(route.RouteCode, routeCode, StringComparison.Ordinal)))
        {
            throw new DomainRuleException("A configuration release can contain only one route with the same code.");
        }

        var route = ExecutionRoute.Create(productVariantId, recipeId, routeCode, priority,
            requiredCapabilitiesJson, supportedOptionCodes);
        _executionRoutes.Add(route);
        return route;
    }

    public void ClearRoutes()
    {
        EnsureDraft();
        _executionRoutes.Clear();
    }

    public IReadOnlyCollection<ExecutionRoute> ReplaceRoutes(
        IEnumerable<(Guid ProductVariantId, Guid RecipeId, string RouteCode, int Priority,
            string? RequiredCapabilitiesJson,
            IReadOnlyCollection<string> SupportedOptionCodes,
            IReadOnlyCollection<(Guid ProductionProgramBindingId, string ProductionProgramBindingChecksum, Guid RobotProgramId, int BindingOrder, IReadOnlyCollection<string> CapabilityCodes)> Bindings)> replacements)
    {
        EnsureDraft();
        var removed = _executionRoutes.ToArray();
        _executionRoutes.Clear();
        foreach (var replacement in replacements)
        {
            var route = AddRoute(replacement.ProductVariantId, replacement.RecipeId, replacement.RouteCode,
                replacement.Priority, replacement.RequiredCapabilitiesJson, replacement.SupportedOptionCodes);
            foreach (var binding in replacement.Bindings)
                route.AddRobotBinding(binding.ProductionProgramBindingId,
                    binding.ProductionProgramBindingChecksum,
                    binding.RobotProgramId, binding.BindingOrder, binding.CapabilityCodes);
        }

        return removed;
    }

    public void Publish(
        DateTimeOffset publishedAt,
        Guid publishedByAccountId,
        IReadOnlyDictionary<Guid, PublishedRobotProgramSnapshot> programSnapshots)
    {
        var manifest = PreparePublication(publishedByAccountId, programSnapshots);
        ManifestJson = manifest.Json;
        ReleaseChecksum = manifest.Checksum;
        PublishedAt = publishedAt;
        PublishedByAccountId = publishedByAccountId;
        Status = ConfigurationReleaseStatus.Published;
    }

    public ConfigurationReleaseManifest PreparePublication(
        Guid publishedByAccountId,
        IReadOnlyDictionary<Guid, PublishedRobotProgramSnapshot> programSnapshots)
    {
        EnsureDraft();

        if (!_executionRoutes.Any())
        {
            throw new DomainRuleException("Cannot publish a configuration release without execution routes.");
        }

        if (_executionRoutes.Any(route => !route.HasRobotBindings))
        {
            throw new DomainRuleException("Every configuration release route must have at least one robot binding before publication.");
        }

        if (publishedByAccountId == Guid.Empty)
        {
            throw new DomainRuleException("Configuration release publisher account id is required.");
        }

        return ConfigurationReleaseManifestBuilder.CreateContent(this, programSnapshots);
    }

    public void Retire(DateTimeOffset retiredAt)
    {
        if (Status == ConfigurationReleaseStatus.Retired)
        {
            return;
        }

        if (Status != ConfigurationReleaseStatus.Published)
        {
            throw new DomainRuleException("Only published configuration releases can be retired.");
        }

        Status = ConfigurationReleaseStatus.Retired;
        RetiredAt = retiredAt;
    }

    private void EnsureDraft()
    {
        if (Status != ConfigurationReleaseStatus.Draft)
        {
            throw new DomainRuleException("Only draft configuration releases can be modified.");
        }
    }

    public void ValidateFullEdgeDeploymentTarget(
        Domain.Devices.ExecutionEndpoints.KioskExecutionEndpoint endpoint,
        Guid edgeRuntimeId,
        bool allowRetiredRelease = false)
    {
        if ((Status != ConfigurationReleaseStatus.Published &&
                !(allowRetiredRelease && Status == ConfigurationReleaseStatus.Retired)) ||
            endpoint.ExecutionProfile != KioskExecutionProfile.FullEdge ||
            endpoint.Status != KioskExecutionEndpointStatus.Active ||
            endpoint.FullEdgeRuntimeId != edgeRuntimeId)
        {
            throw new DomainRuleException("Only a published release can deploy to its active Full Edge endpoint.");
        }

        if (endpoint.Kiosk is null)
        {
            throw new DomainRuleException("Endpoint kiosk must be loaded before validating deployment tenant scope.");
        }

        if (endpoint.Kiosk.OrganizationId != OrganizationId)
        {
            throw new DomainRuleException("Configuration releases can deploy only within their organization.");
        }

        foreach (var route in _executionRoutes)
        {
            ValidateRouteOrganization(route);
            EnsureScopeMatchesKiosk(
                route.ProductVariant.Product.OrganizationId,
                route.ProductVariant.Product.StoreId,
                route.ProductVariant.Product.KioskId,
                endpoint.Kiosk,
                "Product variant");
            EnsureScopeMatchesKiosk(
                route.Recipe.OrganizationId,
                route.Recipe.StoreId,
                route.Recipe.KioskId,
                endpoint.Kiosk,
                "Recipe");

            foreach (var binding in route.RobotBindings)
            {
                if (binding.RobotProgram is null)
                {
                    throw new DomainRuleException("Robot program bindings must be loaded before validating deployment compatibility.");
                }

                EnsureScopeMatchesKiosk(
                    binding.RobotProgram.OrganizationId,
                    binding.RobotProgram.StoreId,
                    binding.RobotProgram.KioskId,
                    endpoint.Kiosk,
                    "Robot program");

                _ = RobotProgramManifestBuilder.Parse(
                    binding.RobotProgram.ProgramManifestJson
                        ?? throw new DomainRuleException("Published robot program manifest is missing."));
            }
        }
    }

    private void ValidateRouteOrganization(ExecutionRoute route)
    {
        if (route.ProductVariant is null || route.ProductVariant.Product is null || route.Recipe is null)
        {
            throw new DomainRuleException("Route product variant, product, and recipe must be loaded before validation.");
        }

        if (route.Recipe.ProductVariantId != route.ProductVariantId)
        {
            throw new DomainRuleException("An execution route recipe must belong to its product variant.");
        }

        EnsureOrganizationScope(route.ProductVariant.Product.OrganizationId, "Product variant");
        EnsureOrganizationScope(route.Recipe.OrganizationId, "Recipe");
    }

    private void EnsureOrganizationScope(Guid? scopedOrganizationId, string resourceName)
    {
        if (scopedOrganizationId.HasValue && scopedOrganizationId.Value != OrganizationId)
        {
            throw new DomainRuleException($"{resourceName} must belong to the configuration release organization or be global.");
        }
    }

    private static void EnsureScopeMatchesKiosk(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Domain.Tenants.Entities.Kiosk targetKiosk,
        string resourceName)
    {
        if ((organizationId.HasValue && organizationId.Value != targetKiosk.OrganizationId) ||
            (storeId.HasValue && storeId.Value != targetKiosk.StoreId) ||
            (kioskId.HasValue && kioskId.Value != targetKiosk.Id))
        {
            throw new DomainRuleException($"{resourceName} scope does not apply to the deployment kiosk.");
        }
    }

}
