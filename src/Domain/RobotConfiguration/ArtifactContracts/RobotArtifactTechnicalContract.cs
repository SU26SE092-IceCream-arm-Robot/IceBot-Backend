using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.Common;

namespace Domain.RobotConfiguration.ArtifactContracts;

public enum RobotArtifactContractStatus
{
    Draft = 0,
    Published = 1,
    Retired = 2
}

public enum RobotArtifactEffectKind
{
    System = 0,
    Ingredient = 1,
    Option = 2,
    Motion = 3,
    Composite = 4
}

public enum RobotArtifactQuantityMode
{
    None = 0,
    FixedInArtifact = 1,
    Parameterized = 2
}

public enum RobotArtifactOrderingConstraintType
{
    Phase = 0,
    BeforeEffect = 1,
    AfterEffect = 2
}

public sealed record RobotArtifactEffectDefinition(
    string EffectCode,
    RobotArtifactEffectKind EffectKind,
    string? IngredientCode,
    string? OptionCode,
    RobotArtifactQuantityMode QuantityMode,
    decimal? FixedQuantity,
    string? Unit,
    string? RequiredWorkcellCapabilityCode);

public sealed record RobotArtifactOrderingConstraintDefinition(
    RobotArtifactOrderingConstraintType ConstraintType,
    string Value,
    int SortHint);

public sealed class RobotArtifactTechnicalContract : BusinessEntity
{
    private readonly List<RobotArtifactDeclaredEffect> _effects = [];
    private readonly List<RobotArtifactOrderingConstraint> _orderingConstraints = [];

    public Guid? OrganizationId { get; private set; }
    public Guid? SourceContractId { get; private set; }
    public string ContractCode { get; private set; } = null!;
    public int ContractVersion { get; private set; }
    public int SchemaVersion { get; private set; } = 1;
    public string RuntimeTargetCode { get; private set; } = null!;
    public string MachineModelCode { get; private set; } = null!;
    public RobotArtifactContractStatus Status { get; private set; }
    public string? ContractJson { get; private set; }
    public string? ContractChecksum { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? RetiredAt { get; private set; }

    public IReadOnlyCollection<RobotArtifactDeclaredEffect> Effects => _effects;
    public IReadOnlyCollection<RobotArtifactOrderingConstraint> OrderingConstraints => _orderingConstraints;

    private RobotArtifactTechnicalContract() { }

    public static RobotArtifactTechnicalContract CreateDraft(
        string contractCode,
        int contractVersion,
        string runtimeTargetCode,
        string machineModelCode,
        Guid? organizationId = null,
        Guid? sourceContractId = null)
    {
        if (contractVersion <= 0)
            throw new DomainRuleException("Robot artifact technical contract version must be positive.");

        return new RobotArtifactTechnicalContract
        {
            OrganizationId = organizationId,
            SourceContractId = sourceContractId,
            ContractCode = RequireCode(contractCode, "Technical contract code"),
            ContractVersion = contractVersion,
            RuntimeTargetCode = RequireCode(runtimeTargetCode, "Runtime target code"),
            MachineModelCode = RequireCode(machineModelCode, "Machine model code")
        };
    }

    public void ReplaceDefinition(
        IReadOnlyCollection<RobotArtifactEffectDefinition> effects,
        IReadOnlyCollection<RobotArtifactOrderingConstraintDefinition> constraints)
    {
        EnsureDraft();
        if (effects.Count == 0)
            throw new DomainRuleException("A robot artifact technical contract requires at least one declared effect.");
        if (effects.GroupBy(x => NormalizeCode(x.EffectCode)).Any(x => x.Count() > 1))
            throw new DomainRuleException("Robot artifact effect codes must be unique within a technical contract.");

        _effects.Clear();
        foreach (var effect in effects.OrderBy(x => NormalizeCode(x.EffectCode), StringComparer.Ordinal))
            _effects.Add(RobotArtifactDeclaredEffect.Create(effect));

        _orderingConstraints.Clear();
        foreach (var constraint in constraints
                     .OrderBy(x => x.ConstraintType)
                     .ThenBy(x => x.SortHint)
                     .ThenBy(x => NormalizeCode(x.Value), StringComparer.Ordinal))
        {
            _orderingConstraints.Add(RobotArtifactOrderingConstraint.Create(constraint));
        }
    }

    public void Publish(DateTimeOffset now, Guid? actorId, bool parameterizedRuntimeSupported)
    {
        EnsureDraft();
        ValidateForPublication(parameterizedRuntimeSupported);

        var document = new
        {
            Id,
            ContractCode,
            ContractVersion,
            SchemaVersion,
            RuntimeTargetCode,
            MachineModelCode,
            Effects = _effects.OrderBy(x => x.EffectCode).Select(x => new
            {
                x.EffectCode,
                x.EffectKind,
                x.IngredientCode,
                x.OptionCode,
                x.QuantityMode,
                x.FixedQuantity,
                x.Unit,
                x.RequiredWorkcellCapabilityCode
            }),
            OrderingConstraints = _orderingConstraints
                .OrderBy(x => x.ConstraintType).ThenBy(x => x.SortHint).ThenBy(x => x.Value)
                .Select(x => new { x.ConstraintType, x.Value, x.SortHint })
        };
        ContractJson = JsonSerializer.Serialize(document);
        ContractChecksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ContractJson))).ToLowerInvariant();
        Status = RobotArtifactContractStatus.Published;
        PublishedAt = now;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    public void ValidateForPublication(bool parameterizedRuntimeSupported)
    {
        EnsureDraft();
        if (_effects.Count == 0)
            throw new DomainRuleException("A robot artifact technical contract requires effects before publication.");
        if (!parameterizedRuntimeSupported && _effects.Any(x => x.QuantityMode == RobotArtifactQuantityMode.Parameterized))
            throw new DomainRuleException("Parameterized artifact quantities are not supported by the current runtime contract.");
    }

    public void Retire(DateTimeOffset now, Guid? actorId)
    {
        if (Status == RobotArtifactContractStatus.Retired) return;
        if (Status != RobotArtifactContractStatus.Published)
            throw new DomainRuleException("Only published technical contracts can be retired.");
        Status = RobotArtifactContractStatus.Retired;
        RetiredAt = now;
        UpdatedAt = now;
        UpdatedByAccountId = actorId;
    }

    private void EnsureDraft()
    {
        if (Status != RobotArtifactContractStatus.Draft)
            throw new DomainRuleException("Only Draft technical contracts can be changed.");
    }

    internal static string RequireCode(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainRuleException($"{name} is required.");
        return NormalizeCode(value);
    }

    internal static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
}

public sealed class RobotArtifactDeclaredEffect : BusinessEntity
{
    public Guid TechnicalContractId { get; private set; }
    public string EffectCode { get; private set; } = null!;
    public RobotArtifactEffectKind EffectKind { get; private set; }
    public string? IngredientCode { get; private set; }
    public string? OptionCode { get; private set; }
    public RobotArtifactQuantityMode QuantityMode { get; private set; }
    public decimal? FixedQuantity { get; private set; }
    public string? Unit { get; private set; }
    public string? RequiredWorkcellCapabilityCode { get; private set; }
    public RobotArtifactTechnicalContract TechnicalContract { get; private set; } = null!;

    private RobotArtifactDeclaredEffect() { }

    internal static RobotArtifactDeclaredEffect Create(RobotArtifactEffectDefinition definition)
    {
        if (definition.QuantityMode == RobotArtifactQuantityMode.FixedInArtifact &&
            (!definition.FixedQuantity.HasValue || definition.FixedQuantity <= 0 || string.IsNullOrWhiteSpace(definition.Unit)))
            throw new DomainRuleException("Fixed-in-artifact effects require positive quantity and unit.");
        if (definition.QuantityMode != RobotArtifactQuantityMode.FixedInArtifact && definition.FixedQuantity.HasValue)
            throw new DomainRuleException("Only fixed-in-artifact effects may declare a fixed quantity.");

        return new RobotArtifactDeclaredEffect
        {
            EffectCode = RobotArtifactTechnicalContract.RequireCode(definition.EffectCode, "Effect code"),
            EffectKind = definition.EffectKind,
            IngredientCode = NormalizeOptional(definition.IngredientCode),
            OptionCode = NormalizeOptional(definition.OptionCode),
            QuantityMode = definition.QuantityMode,
            FixedQuantity = definition.FixedQuantity,
            Unit = string.IsNullOrWhiteSpace(definition.Unit) ? null : definition.Unit.Trim().ToLowerInvariant(),
            RequiredWorkcellCapabilityCode = NormalizeOptional(definition.RequiredWorkcellCapabilityCode)
        };
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}

public sealed class RobotArtifactOrderingConstraint : BusinessEntity
{
    public Guid TechnicalContractId { get; private set; }
    public RobotArtifactOrderingConstraintType ConstraintType { get; private set; }
    public string Value { get; private set; } = null!;
    public int SortHint { get; private set; }
    public RobotArtifactTechnicalContract TechnicalContract { get; private set; } = null!;

    private RobotArtifactOrderingConstraint() { }

    internal static RobotArtifactOrderingConstraint Create(RobotArtifactOrderingConstraintDefinition definition) => new()
    {
        ConstraintType = definition.ConstraintType,
        Value = RobotArtifactTechnicalContract.RequireCode(definition.Value, "Ordering constraint value"),
        SortHint = definition.SortHint
    };
}
