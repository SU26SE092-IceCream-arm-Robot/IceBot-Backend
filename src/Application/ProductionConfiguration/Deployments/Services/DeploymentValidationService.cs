using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Common;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionConfiguration.Entities;

namespace Application.ProductionConfiguration.Deployments.Services;

public sealed record DeploymentValidationReport(
    string Checksum,
    string RiskLevel,
    IReadOnlyCollection<string> WarningCodes,
    bool RequiresAcknowledgement);

public sealed class DeploymentValidationService
{
    public DeploymentValidationReport Build(ConfigurationRelease release, KioskExecutionEndpoint endpoint)
    {
        if (string.IsNullOrWhiteSpace(release.ReleaseChecksum) || release.ExecutionRoutes.Count == 0 ||
            release.ExecutionRoutes.Any(x => string.IsNullOrWhiteSpace(x.ProductionDefinitionChecksum)))
            throw new DomainRuleException("Deployment validation requires a published release with production definitions.");
        var warnings = new[] { "UNPROVEN_PHYSICAL_BEHAVIOR" };
        var json = JsonSerializer.Serialize(new
        {
            SchemaVersion = 1,
            ConfigurationReleaseId = release.Id,
            release.ReleaseChecksum,
            KioskExecutionEndpointId = endpoint.Id,
            endpoint.KioskId,
            endpoint.ExecutionProfile,
            ProductionDefinitions = release.ExecutionRoutes.OrderBy(x => x.RouteCode)
                .Select(x => new { x.Id, x.ProductionDefinitionChecksum }),
            RiskLevel = "UnprovenPhysicalBehavior",
            WarningCodes = warnings
        });
        return new DeploymentValidationReport(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant(),
            "UnprovenPhysicalBehavior", warnings, true);
    }

    public static void ValidateAcknowledgement(
        DeploymentValidationReport report, string? submittedChecksum, bool acknowledged)
    {
        if (!string.Equals(report.Checksum, submittedChecksum?.Trim(), StringComparison.Ordinal))
            throw new DomainRuleException("Deployment validation report is missing or stale. Preview validation again.");
        if (report.RequiresAcknowledgement && !acknowledged)
            throw new DomainRuleException("Authorized organization acknowledgement is required for the remaining deployment risk.");
    }
}
