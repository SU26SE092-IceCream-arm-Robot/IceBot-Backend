using System.Text.Json;
using Application.ProductionConfiguration.Deployments.Results;
using Application.ProductionConfiguration.Deployments.Commands;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Manifests;
using Domain.ProductionConfiguration.ValueObjects;
using Domain.RobotConfiguration.Programs.Manifests;

namespace Application.ProductionConfiguration.Deployments.Services;

public static class DeploymentCommandFactory
{
    public static string BuildFullEdgePayload(
        KioskConfigurationDeployment deployment,
        ConfigurationRelease release,
        FullEdgeReleaseBundleDescriptor fullEdgeBundle,
        Guid? rollbackTargetDeploymentId,
        DateTimeOffset? requestedCommandExpiryAt)
    {
        var artifacts = release.ExecutionRoutes
            .SelectMany(route => route.RobotBindings)
            .SelectMany(binding => RobotProgramManifestBuilder.Parse(
                binding.RobotProgram.ProgramManifestJson
                    ?? throw new DomainRuleException("Published robot program manifest is missing."))
                .Artifacts)
            .Select(programArtifact => programArtifact.RobotArtifact)
            .GroupBy(artifact => artifact.Id)
            .Select(group => group.First())
            .OrderBy(artifact => artifact.Id)
            .Select(artifact => new
            {
                RobotArtifactId = artifact.Id,
                artifact.StorageKey,
                ArtifactChecksum = artifact.Checksum,
                artifact.RuntimeTargetCode,
                artifact.MachineModelCode,
                artifact.ContentLengthBytes
            })
            .ToArray();

        return JsonSerializer.Serialize(new
        {
            DeploymentId = deployment.Id,
            deployment.AttemptNo,
            deployment.KioskId,
            TargetExecutionEndpointId = deployment.KioskExecutionEndpointId,
            deployment.ConfigurationReleaseId,
            deployment.ReleaseChecksum,
            RollbackTargetDeploymentId = rollbackTargetDeploymentId,
            RequestedCommandExpiryAt = requestedCommandExpiryAt,
            release.ReleaseManifestSchemaVersion,
            release.ManifestJson,
            FullEdgeBundle = fullEdgeBundle,
            Artifacts = artifacts
        });
    }

    public static string BuildLowCostPayload(
        ControllerArtifactSetDeployment deployment,
        Guid? rollbackTargetDeploymentId,
        DateTimeOffset? requestedCommandExpiryAt) =>
        JsonSerializer.Serialize(new
        {
            DeploymentId = deployment.Id,
            deployment.KioskId,
            TargetExecutionEndpointId = deployment.KioskExecutionEndpointId,
            deployment.ControllerId,
            ConfigurationReleaseId = deployment.SourceConfigurationReleaseId,
            deployment.ReleaseChecksum,
            RollbackTargetDeploymentId = rollbackTargetDeploymentId,
            RequestedCommandExpiryAt = requestedCommandExpiryAt,
            deployment.ActiveSetVersion,
            deployment.ActiveSetChecksum,
            deployment.MaxArtifactCount,
            deployment.MaxArtifactStorageBytes,
            deployment.RequestedArtifactCount,
            deployment.RequestedArtifactStorageBytes,
            Items = deployment.Items
                .OrderBy(item => item.ExecutionRouteId)
                .ThenBy(item => item.RobotProgramId)
                .ThenBy(item => item.RunOrder)
                .ThenBy(item => item.RobotArtifactId)
                .Select(item => new
                {
                    item.ExecutionRouteId,
                    item.RobotProgramId,
                    item.RobotProgramManifestChecksum,
                    item.RobotArtifactId,
                    item.ArtifactChecksum,
                    item.StorageKey,
                    item.RuntimeTargetCode,
                    item.MachineModelCode,
                    item.DeviceId,
                    item.ContentLengthBytes,
                    item.RunOrder,
                    item.ParametersSchemaVersion,
                    item.ParametersJson,
                    item.RequiredOptionCode
                }).ToArray()
        });

    public static bool LowCostSelectionsMatch(
        ControllerArtifactSetDeployment existing,
        IReadOnlyCollection<DeployLowCostArtifactSelection> requested) =>
        existing.Items.Select(item => (item.ExecutionRouteId, item.RobotProgramId)).Distinct()
            .OrderBy(item => item.ExecutionRouteId).ThenBy(item => item.RobotProgramId)
            .SequenceEqual(requested.Select(item => (item.ExecutionRouteId, item.RobotProgramId)).Distinct()
                .OrderBy(item => item.ExecutionRouteId).ThenBy(item => item.RobotProgramId));

    public static IReadOnlyCollection<ControllerArtifactSetItemSnapshot> MaterializeLowCostItems(
        Domain.Devices.ExecutionEndpoints.KioskExecutionEndpoint endpoint,
        ConfigurationRelease release,
        IReadOnlyCollection<DeployLowCostArtifactSelection> selections)
    {
        var items = new List<ControllerArtifactSetItemSnapshot>(selections.Count);
        foreach (var selection in selections)
        {
            var route = release.ExecutionRoutes.SingleOrDefault(item => item.Id == selection.ExecutionRouteId)
                ?? throw new DomainRuleException("Selected active-set route does not belong to the source release.");
            var binding = route.RobotBindings.SingleOrDefault(item => item.RobotProgramId == selection.RobotProgramId)
                ?? throw new DomainRuleException("Selected active-set program does not belong to the source route.");
            var program = binding.RobotProgram;
            if (!AppliesToKiosk(route.ProductVariant.Product.OrganizationId, route.ProductVariant.Product.StoreId,
                    route.ProductVariant.Product.KioskId, endpoint.Kiosk) ||
                !AppliesToKiosk(route.Recipe.OrganizationId, route.Recipe.StoreId, route.Recipe.KioskId, endpoint.Kiosk) ||
                !AppliesToKiosk(program.OrganizationId, program.StoreId, program.KioskId, endpoint.Kiosk))
                throw new DomainRuleException("Selected route, recipe, and robot program must apply to the target kiosk scope.");

            var programManifest = RobotProgramManifestBuilder.Parse(program.ProgramManifestJson
                ?? throw new DomainRuleException("Selected robot program has no published artifact manifest."));
            foreach (var programArtifact in programManifest.Artifacts.OrderBy(item => item.RunOrder))
            {
                var artifact = programArtifact.RobotArtifact;
                items.Add(new ControllerArtifactSetItemSnapshot(
                    route.Id, program.Id, program.ProgramManifestChecksum!, artifact.Id, artifact.Checksum,
                    artifact.StorageKey, artifact.RuntimeTargetCode, artifact.MachineModelCode, program.DeviceId,
                    artifact.ContentLengthBytes, programArtifact.RunOrder, programArtifact.ParametersSchemaVersion,
                    programArtifact.Parameters?.ToJsonString(), programArtifact.RequiredOptionCode));
            }
        }
        return items;
    }

    private static bool AppliesToKiosk(Guid? organizationId, Guid? storeId, Guid? kioskId,
        Domain.Tenants.Entities.Kiosk kiosk) =>
        (!organizationId.HasValue || organizationId == kiosk.OrganizationId) &&
        (!storeId.HasValue || storeId == kiosk.StoreId) &&
        (!kioskId.HasValue || kioskId == kiosk.Id);
}
