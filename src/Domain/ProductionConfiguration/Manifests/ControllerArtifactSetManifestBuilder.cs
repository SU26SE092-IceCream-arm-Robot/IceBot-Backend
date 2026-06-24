using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.ProductionConfiguration.Entities;

namespace Domain.ProductionConfiguration.Manifests;

public static class ControllerArtifactSetManifestBuilder
{
    public static ControllerArtifactSetManifest Create(ControllerArtifactSetDeployment deployment)
    {
        var document = new
        {
            deployment.KioskExecutionEndpointId,
            deployment.ControllerId,
            deployment.SourceConfigurationReleaseId,
            deployment.ReleaseChecksum,
            deployment.ActiveSetVersion,
            Items = deployment.Items.OrderBy(item => item.ExecutionRouteId).ThenBy(item => item.RobotProgramId).ThenBy(item => item.RunOrder).ThenBy(item => item.RobotArtifactId)
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
                    item.ParametersJson
                }).ToArray()
        };
        var json = JsonSerializer.Serialize(document);
        return new ControllerArtifactSetManifest(json, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant());
    }
}

public sealed record ControllerArtifactSetManifest(string Json, string Checksum);
