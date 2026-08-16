using Application.ProductionConfiguration.Deployments.Services;
using Domain.Common;
using Domain.Devices.ExecutionEndpoints;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class DeploymentRuntimeCompatibilityRulesTests
{
    [Fact]
    public void NoReportedDeviceSnapshot_RemainsAdvisoryForMvp()
    {
        var endpoint = EndpointWithoutReports();

        DeploymentRuntimeCompatibilityRules.EnsureCompatibleWhenReported(
            [("FAIRINO_LUA_V1", "FR5")], endpoint.ReportedDevices);

        Assert.Empty(endpoint.ReportedDevices);
    }

    [Fact]
    public void MatchingReportedDevice_IsCompatible()
    {
        var endpoint = EndpointWithReport("FAIRINO_LUA_V1", "FR5");

        DeploymentRuntimeCompatibilityRules.EnsureCompatibleWhenReported(
            [("fairino_lua_v1", "fr5")], endpoint.ReportedDevices);
    }

    [Fact]
    public void ReportedDeviceMismatch_IsBlocked()
    {
        var endpoint = EndpointWithReport("FAIRINO_LUA_V1", "FR3");

        var exception = Assert.Throws<DomainRuleException>(() =>
            DeploymentRuntimeCompatibilityRules.EnsureCompatibleWhenReported(
                [("FAIRINO_LUA_V1", "FR5")], endpoint.ReportedDevices));

        Assert.Contains("FAIRINO_LUA_V1/FR5", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OverlongReportedDevice_IsRejectedBeforePersistence()
    {
        var endpoint = EndpointWithoutReports();
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<DomainRuleException>(() => endpoint.ApplyReportedDevicesSnapshot(
            Guid.NewGuid(), 1, now, now,
            [new ReportedDeviceSnapshotItem("arm-left", null, new string('R', 101), "FR5")]));
    }

    private static KioskExecutionEndpoint EndpointWithoutReports()
    {
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            Guid.NewGuid(), "EDGE-1", KioskExecutionProfile.FullEdge,
            ExecutionEndpointAuthenticationMode.MutualTls);
        endpoint.Id = Guid.NewGuid();
        return endpoint;
    }

    private static KioskExecutionEndpoint EndpointWithReport(string runtimeTargetCode, string machineModelCode)
    {
        var endpoint = EndpointWithoutReports();
        var now = DateTimeOffset.UtcNow;
        endpoint.ApplyReportedDevicesSnapshot(
            Guid.NewGuid(), 1, now, now,
            [new ReportedDeviceSnapshotItem("arm-left", null, runtimeTargetCode, machineModelCode)]);
        return endpoint;
    }
}
