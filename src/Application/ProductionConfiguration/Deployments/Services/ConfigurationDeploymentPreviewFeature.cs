using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Identity.Tokens.Claims;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Readiness.Services;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Routes.Support;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.ExecutionEndpoints.Projections;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.RobotConfiguration.Programs.Manifests;
using Microsoft.Extensions.Options;
using Application.Devices.Telemetry;

namespace Application.ProductionConfiguration.Deployments.Services;

public interface IConfigurationDeploymentPreviewService
{
    Task<ApiResult<ConfigurationDeploymentPreview>> HandleAsync(
        CurrentUserContext user,
        Guid kioskId,
        Guid releaseId,
        Guid? endpointId,
        IReadOnlyCollection<DeploymentPreviewSelection> requestedSelections,
        CancellationToken cancellationToken,
        bool allowRetiredRelease = false);
}

public sealed record DeploymentPreviewSelection(Guid ExecutionRouteId, Guid RobotProgramId);

public sealed record ConfigurationDeploymentPreview(
    Guid ConfigurationReleaseId,
    string ReleaseChecksum,
    Guid KioskId,
    bool RequiresEndpointSelection,
    IReadOnlyCollection<ConfigurationDeploymentEndpointPreview> Endpoints);

public sealed record ConfigurationDeploymentEndpointPreview(
    Guid KioskExecutionEndpointId,
    string EndpointCode,
    string ExecutionProfile,
    bool IsEligible,
    IReadOnlyCollection<DeploymentPreviewBlocker> Blockers,
    IReadOnlyCollection<DeploymentPreviewSelection> Selections,
    IReadOnlyCollection<DeploymentPreviewArtifact> Artifacts,
    IReadOnlyCollection<string> InstallationModes,
    int ArtifactCount,
    long ArtifactStorageBytes,
    int? MaximumArtifactCount,
    long? MaximumArtifactStorageBytes,
    string DeploymentChecksum,
    DeploymentValidationReport? Validation);

public sealed record DeploymentPreviewArtifact(
    Guid ExecutionRouteId,
    Guid RobotProgramId,
    Guid RobotArtifactId,
    Guid? DeviceId,
    int RunOrder,
    string Checksum,
    string RuntimeTargetCode,
    string MachineModelCode,
    long ContentLengthBytes,
    string? RequiredOptionCode);

public sealed record DeploymentPreviewBlocker(string Code, string Message);

public sealed record DeploymentSelectionResolution(
    IReadOnlyCollection<DeploymentPreviewSelection> Selections,
    IReadOnlyCollection<DeploymentPreviewBlocker> Blockers);

public static class ConfigurationDeploymentPreviewRules
{
    public static DeploymentSelectionResolution ResolveSelections(
        ConfigurationRelease release,
        KioskExecutionProfile profile,
        IReadOnlyCollection<DeploymentPreviewSelection> requested)
    {
        var blockers = new List<DeploymentPreviewBlocker>();
        if (profile == KioskExecutionProfile.FullEdge)
        {
            if (requested.Count > 0)
                blockers.Add(new("SelectionsNotApplicable",
                    "Full Edge deployment installs the complete release and does not accept route/program selections."));
            var completeRelease = release.ExecutionRoutes
                .OrderBy(route => route.Priority).ThenBy(route => route.RouteCode)
                .SelectMany(route => route.RobotBindings.OrderBy(binding => binding.BindingOrder)
                    .Select(binding => new DeploymentPreviewSelection(route.Id, binding.RobotProgramId)))
                .ToArray();
            return new(completeRelease, blockers);
        }

        if (requested.Count > 0)
        {
            if (requested.GroupBy(item => item.ExecutionRouteId).Any(group => group.Count() > 1))
                blockers.Add(new("DuplicateRouteSelection", "Each execution route can select only one robot program."));
            return new(requested.Distinct().ToArray(), blockers);
        }

        var selections = new List<DeploymentPreviewSelection>();
        foreach (var route in release.ExecutionRoutes.OrderBy(item => item.Priority).ThenBy(item => item.RouteCode))
        {
            if (route.RobotBindings.Count != 1)
            {
                blockers.Add(new("ProgramSelectionRequired",
                    $"Execution route '{route.RouteCode}' requires an explicit robot-program selection."));
                continue;
            }
            selections.Add(new(route.Id, route.RobotBindings.Single().RobotProgramId));
        }
        return new(selections, blockers);
    }
}

public sealed class ConfigurationDeploymentPreviewHandler(
    IConfigurationReleaseStore releases,
    IConfigurationDeploymentStore deployments,
    ProductionInventoryReadinessGuard inventoryReadiness,
    DeploymentValidationService validation,
    IOptions<LowCostControllerCapacityOptions> capacityOptions,
    IOptions<EdgeTelemetryIngestionOptions> telemetryOptions) : IConfigurationDeploymentPreviewService
{
    private readonly LowCostControllerCapacityOptions _capacity = capacityOptions.Value;
    private readonly EdgeTelemetryIngestionOptions _telemetry = telemetryOptions.Value;

    public async Task<ApiResult<ConfigurationDeploymentPreview>> HandleAsync(
        CurrentUserContext user,
        Guid kioskId,
        Guid releaseId,
        Guid? endpointId,
        IReadOnlyCollection<DeploymentPreviewSelection> requestedSelections,
        CancellationToken cancellationToken,
        bool allowRetiredRelease = false)
    {
        if (kioskId == Guid.Empty || releaseId == Guid.Empty)
            return ApiResult<ConfigurationDeploymentPreview>.Fail("Kiosk and configuration release are required.", 400);

        var release = await releases.GetReleaseByIdAsync(releaseId, cancellationToken);
        if (release is null ||
            (release.Status != ConfigurationReleaseStatus.Published &&
             !(allowRetiredRelease && release.Status == ConfigurationReleaseStatus.Retired)) ||
            string.IsNullOrWhiteSpace(release.ReleaseChecksum))
            return ApiResult<ConfigurationDeploymentPreview>.Fail("Published configuration release not found.", 404);

        var endpoints = await deployments.ListEndpointsForDeploymentAsync(kioskId, cancellationToken);
        if (endpointId.HasValue)
            endpoints = endpoints.Where(endpoint => endpoint.Id == endpointId.Value).ToArray();
        if (endpoints.Count == 0)
            return ApiResult<ConfigurationDeploymentPreview>.Fail("No execution endpoint was found for the kiosk.", 404);

        var first = endpoints[0];
        if (first.Kiosk.OrganizationId != release.OrganizationId)
            return ApiResult<ConfigurationDeploymentPreview>.Fail("Configuration release does not belong to the kiosk organization.", 400);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleaseDeploy, user,
                first.Kiosk.OrganizationId, first.Kiosk.StoreId, kioskId))
            return ApiResult<ConfigurationDeploymentPreview>.Fail("Access denied.", 403);

        var readinessByEndpoint = (await deployments.ListEndpointReadinessAsync(
                endpoints.Select(endpoint => endpoint.Id),
                DateTimeOffset.UtcNow.AddSeconds(-_telemetry.ReadinessTimeoutSeconds),
                cancellationToken))
            .ToDictionary(item => item.KioskExecutionEndpointId);
        var fullInventory = await inventoryReadiness.EvaluateDeployAsync(
            release, kioskId, cancellationToken: cancellationToken);
        var selectedInventory = requestedSelections.Count == 0
            ? fullInventory
            : await inventoryReadiness.EvaluateDeployAsync(release, kioskId,
                requestedSelections.Select(item => item.ExecutionRouteId).Distinct().ToArray(), cancellationToken);

        var results = endpoints.Select(endpoint => BuildEndpointPreview(
            release,
            endpoint,
            readinessByEndpoint.GetValueOrDefault(endpoint.Id),
            requestedSelections,
            endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge ? fullInventory : selectedInventory,
            allowRetiredRelease)).ToArray();
        var eligibleCount = results.Count(item => item.IsEligible);
        return ApiResult<ConfigurationDeploymentPreview>.Success(new ConfigurationDeploymentPreview(
            release.Id,
            release.ReleaseChecksum,
            kioskId,
            !endpointId.HasValue && eligibleCount > 1,
            results));
    }

    private ConfigurationDeploymentEndpointPreview BuildEndpointPreview(
        ConfigurationRelease release,
        KioskExecutionEndpoint endpoint,
        ExecutionEndpointReadinessProjection? readiness,
        IReadOnlyCollection<DeploymentPreviewSelection> requestedSelections,
        ProductionInventoryReadinessAssessment inventory,
        bool allowRetiredRelease)
    {
        var blockers = new List<DeploymentPreviewBlocker>();
        var selectionResolution = ConfigurationDeploymentPreviewRules.ResolveSelections(
            release, endpoint.ExecutionProfile, requestedSelections);
        blockers.AddRange(selectionResolution.Blockers);
        var selections = selectionResolution.Selections;
        var artifacts = MaterializeArtifacts(release, selections, blockers);

        if (endpoint.Status != KioskExecutionEndpointStatus.Active)
            blockers.Add(new("EndpointNotActive", "Execution endpoint is not Active."));
        if (readiness is null)
            blockers.Add(new("ReadinessNotReported", "Execution endpoint has not reported readiness."));
        else
        {
            if (readiness.Readiness != ExecutionReadinessState.Ready)
                blockers.Add(new("EndpointNotReady", $"Execution endpoint readiness is {readiness.Readiness}."));
            if (readiness.Activity != ExecutionActivityState.Idle)
                blockers.Add(new("EndpointBusy", $"Execution endpoint activity is {readiness.Activity}."));
            if (readiness.Safety != ExecutionSafetyState.Safe)
                blockers.Add(new("SafetyNotReady", $"Execution endpoint safety is {readiness.Safety}."));
            ValidateCapabilities(release, selections, readiness, blockers);
        }

        if (inventory.IsBlocked)
            blockers.Add(new("InventoryNotReady", "Kiosk inventory readiness policy blocks this deployment."));

        ValidateEndpointTarget(release, endpoint, artifacts, blockers, allowRetiredRelease);

        int? maximumCount = null;
        long? maximumBytes = null;
        var modes = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? new[] { "BundleInstall", "IncrementalInstall" }
            : new[] { "LimitedActiveArtifactSet" };
        if (endpoint.ExecutionProfile == KioskExecutionProfile.LowCostController)
        {
            maximumCount = _capacity.MaxArtifactCount;
            maximumBytes = _capacity.MaxArtifactStorageBytes;
            if (artifacts.Select(item => item.RobotArtifactId).Distinct().Count() > maximumCount)
                blockers.Add(new("ArtifactCountExceeded", "Selected programs exceed the controller artifact-count capacity."));
            if (artifacts.GroupBy(item => item.RobotArtifactId).Sum(group => group.First().ContentLengthBytes) > maximumBytes)
                blockers.Add(new("ArtifactStorageExceeded", "Selected programs exceed the controller artifact-storage capacity."));
        }

        var uniqueArtifacts = artifacts.GroupBy(item => item.RobotArtifactId).Select(group => group.First()).ToArray();
        var checksum = BuildChecksum(release, endpoint, selections, artifacts);
        DeploymentValidationReport? report = null;
        try { report = validation.Build(release, endpoint); }
        catch (DomainRuleException ex) { blockers.Add(new("DeploymentValidationFailed", ex.Message)); }

        return new ConfigurationDeploymentEndpointPreview(
            endpoint.Id,
            endpoint.EndpointCode,
            endpoint.ExecutionProfile.ToString(),
            blockers.Count == 0,
            blockers.DistinctBy(item => (item.Code, item.Message)).ToArray(),
            selections,
            artifacts,
            modes,
            uniqueArtifacts.Length,
            uniqueArtifacts.Sum(item => item.ContentLengthBytes),
            maximumCount,
            maximumBytes,
            checksum,
            report);
    }

    private static IReadOnlyCollection<DeploymentPreviewArtifact> MaterializeArtifacts(
        ConfigurationRelease release,
        IReadOnlyCollection<DeploymentPreviewSelection> selections,
        ICollection<DeploymentPreviewBlocker> blockers)
    {
        var artifacts = new List<DeploymentPreviewArtifact>();
        foreach (var selection in selections)
        {
            var route = release.ExecutionRoutes.SingleOrDefault(item => item.Id == selection.ExecutionRouteId);
            var binding = route?.RobotBindings.SingleOrDefault(item => item.RobotProgramId == selection.RobotProgramId);
            if (route is null || binding is null)
            {
                blockers.Add(new("SelectionNotInRelease", "A selected route/program pair does not belong to the release."));
                continue;
            }

            try
            {
                var manifest = RobotProgramManifestBuilder.Parse(binding.RobotProgram.ProgramManifestJson!);
                artifacts.AddRange(manifest.Artifacts.Select(item => new DeploymentPreviewArtifact(
                    route.Id, binding.RobotProgramId, item.RobotArtifact.Id, binding.RobotProgram.DeviceId, item.RunOrder,
                    item.RobotArtifact.Checksum, item.RobotArtifact.RuntimeTargetCode,
                    item.RobotArtifact.MachineModelCode, item.RobotArtifact.ContentLengthBytes,
                    item.RequiredOptionCode)));
            }
            catch (DomainRuleException ex)
            {
                blockers.Add(new("ProgramManifestInvalid", ex.Message));
            }
        }
        return artifacts;
    }

    private static void ValidateEndpointTarget(
        ConfigurationRelease release,
        KioskExecutionEndpoint endpoint,
        IReadOnlyCollection<DeploymentPreviewArtifact> artifacts,
        ICollection<DeploymentPreviewBlocker> blockers,
        bool allowRetiredRelease)
    {
        if (endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge)
        {
            if (!endpoint.FullEdgeRuntimeId.HasValue)
                blockers.Add(new("ExecutionIdentityMissing", "Full Edge runtime identity is missing."));
            else
            {
                try
                {
                    release.ValidateFullEdgeDeploymentTarget(
                        endpoint,
                        endpoint.FullEdgeRuntimeId.Value,
                        allowRetiredRelease);
                }
                catch (DomainRuleException ex) { blockers.Add(new("EndpointIncompatible", ex.Message)); }
            }
        }
        else if (!endpoint.ControllerId.HasValue)
            blockers.Add(new("ExecutionIdentityMissing", "Low-cost controller identity is missing."));

        if (artifacts.Any(artifact => !endpoint.SupportsRobotTarget(
                artifact.RuntimeTargetCode, artifact.MachineModelCode, artifact.DeviceId)))
            blockers.Add(new("RobotTargetMismatch", "Execution endpoint does not support every selected artifact target."));
    }

    private static void ValidateCapabilities(
        ConfigurationRelease release,
        IReadOnlyCollection<DeploymentPreviewSelection> selections,
        ExecutionEndpointReadinessProjection readiness,
        ICollection<DeploymentPreviewBlocker> blockers)
    {
        var available = readiness.Capabilities.Where(item => item.IsAvailable)
            .Select(item => item.CapabilityCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var selection in selections)
        {
            var route = release.ExecutionRoutes.SingleOrDefault(item => item.Id == selection.ExecutionRouteId);
            var binding = route?.RobotBindings.SingleOrDefault(item => item.RobotProgramId == selection.RobotProgramId);
            if (binding is not null && !available.Contains(binding.RequiredWorkcellCapabilityCode))
                blockers.Add(new("CapabilityMissing",
                    $"Required capability '{binding.RequiredWorkcellCapabilityCode}' is unavailable."));
            if (route is null || string.IsNullOrWhiteSpace(route.RequiredCapabilitiesJson)) continue;
            try
            {
                foreach (var requirement in ExecutionRouteRequiredCapabilitiesContract.ParseValidated(
                             route.RequiredCapabilitiesJson))
                {
                    if (requirement.Required && !available.Contains(requirement.Code))
                        blockers.Add(new("CapabilityMissing",
                            $"Required capability '{requirement.Code}' is unavailable."));
                    if (requirement.Required && requirement.MinVersion is not null)
                        blockers.Add(new("CapabilityVersionUnverifiable",
                            $"Capability '{requirement.Code}' requires minimum version '{requirement.MinVersion}', " +
                            "but endpoint readiness does not report capability versions."));
                }
            }
            catch (JsonException ex)
            {
                blockers.Add(new("CapabilityContractInvalid", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                blockers.Add(new("CapabilityContractInvalid", ex.Message));
            }
            catch (KeyNotFoundException ex)
            {
                blockers.Add(new("CapabilityContractInvalid", ex.Message));
            }
        }
    }

    private static string BuildChecksum(
        ConfigurationRelease release,
        KioskExecutionEndpoint endpoint,
        IReadOnlyCollection<DeploymentPreviewSelection> selections,
        IReadOnlyCollection<DeploymentPreviewArtifact> artifacts)
    {
        var payload = JsonSerializer.Serialize(new
        {
            SchemaVersion = 1,
            ConfigurationReleaseId = release.Id,
            release.ReleaseChecksum,
            KioskExecutionEndpointId = endpoint.Id,
            endpoint.ExecutionProfile,
            Selections = selections.OrderBy(item => item.ExecutionRouteId).ThenBy(item => item.RobotProgramId),
            Artifacts = artifacts.OrderBy(item => item.ExecutionRouteId).ThenBy(item => item.RobotProgramId)
                .ThenBy(item => item.RunOrder).ThenBy(item => item.RobotArtifactId)
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
