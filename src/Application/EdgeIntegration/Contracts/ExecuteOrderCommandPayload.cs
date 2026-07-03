using System.Text.Json;
using Domain.Common;

namespace Application.EdgeIntegration.Contracts;

public sealed record ExecuteOrderCommandPayload
{
    public Guid CommandId { get; init; }
    public int DispatchAttemptNo { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid KioskId { get; init; }
    public Guid TargetExecutionEndpointId { get; init; }
    public string ExecutionProfile { get; init; } = string.Empty;
    public Guid ConfigurationReleaseId { get; init; }
    public string ReleaseChecksum { get; init; } = string.Empty;
    public int ReleaseManifestSchemaVersion { get; init; }
    public string ManifestJson { get; init; } = string.Empty;
    public long? ActiveSetVersion { get; init; }
    public string? ActiveSetChecksum { get; init; }
    public DateTimeOffset CommandExpiryAt { get; init; }
    public IReadOnlyList<ExecuteOrderLinePayload> OrderLines { get; init; } = [];
}

public sealed record ExecuteOrderLinePayload
{
    public Guid OrderItemId { get; init; }
    public Guid ProductId { get; init; }
    public Guid ProductVariantId { get; init; }
    public Guid? RecipeId { get; init; }
    public int Quantity { get; init; }
    public string ProductCodeSnapshot { get; init; } = string.Empty;
    public string ProductVariantCodeSnapshot { get; init; } = string.Empty;
    public int? RecipeVersionSnapshot { get; init; }
    public int RecipeSnapshotSchemaVersion { get; init; }
    public string? RecipeSnapshotJson { get; init; }
    public int OptionsSchemaVersion { get; init; }
    public string? OptionsJson { get; init; }
    public Guid ExecutionRouteId { get; init; }
    public string RouteCode { get; init; } = string.Empty;
    public string? RequiredCapabilitiesJson { get; init; }
    public IReadOnlyList<ExecuteOrderRobotProgramPayload> RobotPrograms { get; init; } = [];
}

public sealed record ExecuteOrderRobotProgramPayload
{
    public int BindingOrder { get; init; }
    public string RequiredWorkcellCapabilityCode { get; init; } = string.Empty;
    public Guid RobotProgramId { get; init; }
    public int ProgramManifestSchemaVersion { get; init; }
    public string ProgramManifestChecksum { get; init; } = string.Empty;
    public IReadOnlyList<ExecuteOrderArtifactPayload> Artifacts { get; init; } = [];
}

public sealed record ExecuteOrderArtifactPayload
{
    public Guid RobotArtifactId { get; init; }
    public int RunOrder { get; init; }
    public int ParametersSchemaVersion { get; init; }
    public string? ParametersJson { get; init; }
    public string ArtifactChecksum { get; init; } = string.Empty;
    public string RuntimeTargetCode { get; init; } = string.Empty;
    public string MachineModelCode { get; init; } = string.Empty;
}

public static class ExecuteOrderCommandPayloadCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = false
    };

    public static string Serialize(ExecuteOrderCommandPayload payload) =>
        JsonSerializer.Serialize(payload, JsonOptions);

    public static ExecuteOrderCommandPayload Deserialize(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<ExecuteOrderCommandPayload>(payloadJson, JsonOptions)
                ?? throw new DomainRuleException("Execute-order command payload is empty.");
            if (payload.ConfigurationReleaseId == Guid.Empty || string.IsNullOrWhiteSpace(payload.ReleaseChecksum))
            {
                throw new DomainRuleException("Execute-order command payload is missing required identity or release provenance.");
            }

            return payload;
        }
        catch (JsonException ex)
        {
            throw new DomainRuleException($"Execute-order command payload is invalid: {ex.Message}");
        }
    }
}
