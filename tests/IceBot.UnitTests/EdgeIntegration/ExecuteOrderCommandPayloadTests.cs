using Application.EdgeIntegration.Dispatch.Contracts;
using Application.EdgeIntegration.Reports.Contracts;
using Domain.Common;
using Domain.RobotConfiguration.Programs;
using System.Text.Json.Nodes;

namespace IceBot.UnitTests.EdgeIntegration;

public sealed class ExecuteOrderCommandPayloadTests
{
    [Fact]
    public void ReadProvenance_AcceptsLegacyMinimalPayload_WithoutClaimingItIsExecutable()
    {
        var releaseId = Guid.NewGuid();
        var json = $$"""{"ConfigurationReleaseId":"{{releaseId}}","ReleaseChecksum":"abc"}""";

        var provenance = ExecuteOrderCommandPayloadCodec.ReadProvenance(json);

        Assert.Equal(releaseId, provenance.ConfigurationReleaseId);
        Assert.Equal("abc", provenance.ReleaseChecksum);
        Assert.Throws<DomainRuleException>(() => ExecuteOrderCommandPayloadCodec.DeserializeAndValidateFull(json));
    }

    [Fact]
    public void FullPayload_RoundTripsAfterStructuralValidation()
    {
        var payload = new ExecuteOrderCommandPayload
        {
            CommandId = Guid.NewGuid(),
            DispatchAttemptNo = 1,
            OrderId = Guid.NewGuid(),
            OrderNumber = "ORDER-1",
            KioskId = Guid.NewGuid(),
            TargetExecutionEndpointId = Guid.NewGuid(),
            ExecutionProfile = "FullEdge",
            ConfigurationReleaseId = Guid.NewGuid(),
            ReleaseChecksum = "release-checksum",
            ReleaseManifestSchemaVersion = 1,
            ManifestJson = "{}",
            CommandExpiryAt = DateTimeOffset.UtcNow.AddMinutes(5),
            OrderLines =
            [
                new ExecuteOrderLinePayload
                {
                    OrderItemId = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    ProductVariantId = Guid.NewGuid(),
                    RecipeId = Guid.NewGuid(),
                    Quantity = 1,
                    ProductCodeSnapshot = "P",
                    ProductVariantCodeSnapshot = "V",
                    RecipeSnapshotSchemaVersion = 1,
                    SelectedOptions =
                    [
                        new ExecuteOrderLineOptionPayload
                        {
                            ProductOptionId = Guid.NewGuid(),
                            OptionGroupId = 1,
                            OptionGroupCode = "TOPPING",
                            Code = "OREO",
                            Name = "Oreo",
                            UnitPriceDelta = 5000
                        }
                    ],
                    ExecutionRouteId = Guid.NewGuid(),
                    RouteCode = "ROUTE",
                    RobotPrograms =
                    [
                        new ExecuteOrderRobotProgramPayload
                        {
                            BindingOrder = 1,
                            RequiredCapabilityCodes = ["ICE_CREAM"],
                            RobotProgramId = Guid.NewGuid(),
                            ProgramManifestSchemaVersion = 1,
                            ProgramManifestChecksum = "program-checksum",
                            Artifacts =
                            [
                                new ExecuteOrderArtifactPayload
                                {
                                    RobotArtifactId = Guid.NewGuid(),
                                    RunOrder = 1,
                                    ParametersSchemaVersion = 1,
                                    ArtifactChecksum = "artifact-checksum",
                                    RuntimeTargetCode = "FairinoLuaV1",
                                    MachineModelCode = "FR5"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var json = ExecuteOrderCommandPayloadCodec.Serialize(payload);
        var restored = ExecuteOrderCommandPayloadCodec.DeserializeAndValidateFull(json);

        Assert.Equal(5, restored.SchemaVersion);
        Assert.Equal(payload.CommandId, restored.CommandId);
        Assert.Single(restored.OrderLines);
        Assert.All(restored.OrderLines.SelectMany(line => line.RobotPrograms),
            program => Assert.Equal(RobotProgramRestartPolicy.ManualOnly, program.RestartPolicy));
    }

    [Fact]
    public void FullPayload_RejectsIncompleteActiveArtifactSetProvenance()
    {
        var payload = CreateValidPayload() with
        {
            ActiveSetVersion = 7,
            ActiveSetChecksum = null
        };

        var exception = Assert.Throws<DomainRuleException>(() =>
            ExecuteOrderCommandPayloadCodec.Serialize(payload));

        Assert.Equal(
            "Execute-order command payload has incomplete active artifact-set provenance.",
            exception.Message);
    }

    [Fact]
    public void LegacySchema3Payload_WithoutRestartPolicy_DefaultsToManualOnly()
    {
        var node = JsonNode.Parse(ExecuteOrderCommandPayloadCodec.Serialize(CreateValidPayload()))!.AsObject();
        node[nameof(ExecuteOrderCommandPayload.SchemaVersion)] = 3;
        var program = node[nameof(ExecuteOrderCommandPayload.OrderLines)]![0]!
            [nameof(ExecuteOrderLinePayload.RobotPrograms)]![0]!.AsObject();
        program.Remove(nameof(ExecuteOrderRobotProgramPayload.RestartPolicy));

        var restored = ExecuteOrderCommandPayloadCodec.DeserializeAndValidateFull(node.ToJsonString());

        Assert.Equal(
            RobotProgramRestartPolicy.ManualOnly,
            restored.OrderLines[0].RobotPrograms[0].RestartPolicy);
    }

    [Fact]
    public void FullPayload_RejectsUnsupportedRestartPolicy()
    {
        var payload = CreateValidPayload() with
        {
            OrderLines =
            [
                CreateValidPayload().OrderLines[0] with
                {
                    RobotPrograms =
                    [
                        CreateValidPayload().OrderLines[0].RobotPrograms[0] with
                        {
                            RestartPolicy = RobotProgramRestartPolicy.ResumeFromCheckpoint
                        }
                    ]
                }
            ]
        };

        var exception = Assert.Throws<DomainRuleException>(() =>
            ExecuteOrderCommandPayloadCodec.Serialize(payload));

        Assert.Equal(
            "Execute-order command payload contains an invalid order line or robot program manifest.",
            exception.Message);
    }

    private static ExecuteOrderCommandPayload CreateValidPayload() => new()
    {
        CommandId = Guid.NewGuid(),
        DispatchAttemptNo = 1,
        OrderId = Guid.NewGuid(),
        OrderNumber = "ORDER-2",
        KioskId = Guid.NewGuid(),
        TargetExecutionEndpointId = Guid.NewGuid(),
        ExecutionProfile = "LowCostController",
        ConfigurationReleaseId = Guid.NewGuid(),
        ReleaseChecksum = "release-checksum",
        ReleaseManifestSchemaVersion = 1,
        ManifestJson = "{}",
        CommandExpiryAt = DateTimeOffset.UtcNow.AddMinutes(5),
        OrderLines =
        [
            new ExecuteOrderLinePayload
            {
                OrderItemId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductVariantId = Guid.NewGuid(),
                Quantity = 1,
                ProductCodeSnapshot = "P",
                ProductVariantCodeSnapshot = "V",
                RecipeSnapshotSchemaVersion = 1,
                ExecutionRouteId = Guid.NewGuid(),
                RouteCode = "ROUTE",
                RobotPrograms =
                [
                    new ExecuteOrderRobotProgramPayload
                    {
                        BindingOrder = 1,
                        RobotProgramId = Guid.NewGuid(),
                        ProgramManifestChecksum = "program-checksum",
                        Artifacts =
                        [
                            new ExecuteOrderArtifactPayload
                            {
                                RobotArtifactId = Guid.NewGuid(),
                                RunOrder = 1,
                                ArtifactChecksum = "artifact-checksum"
                            }
                        ]
                    }
                ]
            }
        ]
    };
}
