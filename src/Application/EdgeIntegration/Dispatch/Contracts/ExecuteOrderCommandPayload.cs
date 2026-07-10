using System.Text.Json;
using Domain.Common;

namespace Application.EdgeIntegration.Dispatch.Contracts;

public sealed record ExecuteOrderCommandPayload
{
    public int SchemaVersion { get; init; } = 2;
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
    public IReadOnlyList<ExecuteOrderLineOptionPayload> SelectedOptions { get; init; } = [];
    public Guid ExecutionRouteId { get; init; }
    public string RouteCode { get; init; } = string.Empty;
    public string? RequiredCapabilitiesJson { get; init; }
    public IReadOnlyList<ExecuteOrderRobotProgramPayload> RobotPrograms { get; init; } = [];
}

public sealed record ExecuteOrderLineOptionPayload
{
    public Guid ProductOptionId { get; init; }
    public long OptionGroupId { get; init; }
    public string OptionGroupCode { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal UnitPriceDelta { get; init; }
    public IReadOnlyList<ExecuteOrderOptionIngredientRequirementPayload> IngredientRequirements { get; init; } = [];
}

public sealed record ExecuteOrderOptionIngredientRequirementPayload
{
    public Guid IngredientId { get; init; }
    public string IngredientCode { get; init; } = string.Empty;
    public string IngredientName { get; init; } = string.Empty;
    public decimal QuantityPerOption { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string RequiredWorkcellCapabilityCode { get; init; } = string.Empty;
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

    public static string Serialize(ExecuteOrderCommandPayload payload)
    {
        ValidateFull(payload);
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static ExecuteOrderCommandProvenance ReadProvenance(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<ExecuteOrderCommandProvenance>(payloadJson, JsonOptions)
                ?? throw new DomainRuleException("Execute-order command payload is empty.");
            if (payload.ConfigurationReleaseId == Guid.Empty || string.IsNullOrWhiteSpace(payload.ReleaseChecksum))
                throw new DomainRuleException("Execute-order command payload is missing release provenance.");

            return payload;
        }
        catch (JsonException ex)
        {
            throw new DomainRuleException($"Execute-order command payload is invalid: {ex.Message}");
        }
    }

    public static ExecuteOrderCommandPayload DeserializeAndValidateFull(string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<ExecuteOrderCommandPayload>(payloadJson, JsonOptions)
                ?? throw new DomainRuleException("Execute-order command payload is empty.");
            ValidateFull(payload);
            return payload;
        }
        catch (JsonException ex)
        {
            throw new DomainRuleException($"Execute-order command payload is invalid: {ex.Message}");
        }
    }

    private static void ValidateFull(ExecuteOrderCommandPayload payload)
    {
        if (payload.SchemaVersion != 2)
            throw new DomainRuleException("Execute-order command payload schema version is unsupported.");
        if (payload.CommandId == Guid.Empty || payload.DispatchAttemptNo <= 0 || payload.OrderId == Guid.Empty ||
            payload.KioskId == Guid.Empty || payload.TargetExecutionEndpointId == Guid.Empty ||
            string.IsNullOrWhiteSpace(payload.OrderNumber) || string.IsNullOrWhiteSpace(payload.ExecutionProfile) ||
            payload.ConfigurationReleaseId == Guid.Empty || string.IsNullOrWhiteSpace(payload.ReleaseChecksum) ||
            payload.CommandExpiryAt == default || payload.OrderLines.Count == 0)
            throw new DomainRuleException("Execute-order command payload is missing required command identity or execution data.");

        if (payload.OrderLines.Any(line => line.OrderItemId == Guid.Empty || line.ProductId == Guid.Empty ||
                line.ProductVariantId == Guid.Empty || line.Quantity <= 0 || line.ExecutionRouteId == Guid.Empty ||
                line.SelectedOptions.Select(option => option.ProductOptionId).Distinct().Count() != line.SelectedOptions.Count ||
                line.SelectedOptions.Any(option => option.ProductOptionId == Guid.Empty || option.OptionGroupId <= 0 ||
                    string.IsNullOrWhiteSpace(option.OptionGroupCode) || string.IsNullOrWhiteSpace(option.Code) ||
                    string.IsNullOrWhiteSpace(option.Name) || option.UnitPriceDelta < 0 ||
                    option.IngredientRequirements.Select(requirement => requirement.IngredientId).Distinct().Count() != option.IngredientRequirements.Count ||
                    option.IngredientRequirements.Any(requirement => requirement.IngredientId == Guid.Empty ||
                        string.IsNullOrWhiteSpace(requirement.IngredientCode) || string.IsNullOrWhiteSpace(requirement.IngredientName) ||
                        requirement.QuantityPerOption <= 0 || string.IsNullOrWhiteSpace(requirement.Unit) ||
                        string.IsNullOrWhiteSpace(requirement.RequiredWorkcellCapabilityCode))) ||
                line.RobotPrograms.Count == 0 || line.RobotPrograms.Any(program =>
                    program.RobotProgramId == Guid.Empty || program.BindingOrder <= 0 || program.Artifacts.Count == 0 ||
                    program.Artifacts.Any(artifact => artifact.RobotArtifactId == Guid.Empty || artifact.RunOrder <= 0 ||
                        string.IsNullOrWhiteSpace(artifact.ArtifactChecksum)))))
            throw new DomainRuleException("Execute-order command payload contains an invalid order line or robot program manifest.");
    }
}

public sealed record ExecuteOrderCommandProvenance
{
    public int SchemaVersion { get; init; }
    public Guid ConfigurationReleaseId { get; init; }
    public string ReleaseChecksum { get; init; } = string.Empty;
}
