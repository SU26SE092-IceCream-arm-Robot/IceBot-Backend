using Domain.Common;
using Domain.ProductionConfiguration.Manifests;
using Domain.ProductionConfiguration.Enums;
using Domain.Tenants.Entities;

namespace Domain.ProductionConfiguration.Entities;

public class ConfigurationRelease : BusinessEntity
{
    private readonly List<ExecutionRoute> _executionRoutes = [];
    private readonly List<KioskConfigurationDeployment> _kioskConfigurationDeployments = [];

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

    public IReadOnlyCollection<KioskConfigurationDeployment> KioskConfigurationDeployments => _kioskConfigurationDeployments;

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
        string? requiredCapabilitiesJson = null)
    {
        EnsureDraft();

        if (_executionRoutes.Any(route => string.Equals(route.RouteCode, routeCode, StringComparison.Ordinal)))
        {
            throw new DomainRuleException("A configuration release can contain only one route with the same code.");
        }

        var route = ExecutionRoute.Create(productVariantId, recipeId, routeCode, priority, requiredCapabilitiesJson);
        _executionRoutes.Add(route);
        return route;
    }

    public void ClearRoutes()
    {
        EnsureDraft();
        _executionRoutes.Clear();
    }

    public void Publish(DateTimeOffset publishedAt, Guid publishedByAccountId)
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

        foreach (var route in _executionRoutes)
        {
            ValidateRouteOrganization(route);
        }

        var manifest = ConfigurationReleaseManifestBuilder.Create(this);
        ManifestJson = manifest.Json;
        ReleaseChecksum = manifest.Checksum;
        PublishedAt = publishedAt;
        PublishedByAccountId = publishedByAccountId;
        Status = ConfigurationReleaseStatus.Published;
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
        Domain.Devices.Entities.KioskExecutionEndpoint endpoint,
        Guid edgeRuntimeId,
        bool allowRetiredRelease = false)
    {
        if ((Status != ConfigurationReleaseStatus.Published &&
                !(allowRetiredRelease && Status == ConfigurationReleaseStatus.Retired)) ||
            endpoint.ExecutionProfile != Domain.Devices.Enums.KioskExecutionProfile.FullEdge ||
            endpoint.Status != Domain.Devices.Enums.KioskExecutionEndpointStatus.Active ||
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

                foreach (var programArtifact in binding.RobotProgram.RobotProgramArtifacts)
                {
                    if (programArtifact.RobotArtifact is null)
                    {
                        throw new DomainRuleException("Robot program artifacts must be loaded before validating deployment compatibility.");
                    }

                    if (!endpoint.SupportsRobotTarget(
                            programArtifact.RobotArtifact.RuntimeTargetCode,
                            programArtifact.RobotArtifact.MachineModelCode,
                            binding.RobotProgram.DeviceId))
                    {
                        throw new DomainRuleException("The execution endpoint does not support a robot artifact required by this release.");
                    }
                }
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
