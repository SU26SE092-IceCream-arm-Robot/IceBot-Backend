using Application.EdgeIntegration.Dispatch.Contracts;
using Application.EdgeIntegration.Reports.Contracts;
using Domain.Common;

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
                            RequiredWorkcellCapabilityCode = "ICE_CREAM",
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

        Assert.Equal(1, restored.SchemaVersion);
        Assert.Equal(payload.CommandId, restored.CommandId);
        Assert.Single(restored.OrderLines);
    }
}
