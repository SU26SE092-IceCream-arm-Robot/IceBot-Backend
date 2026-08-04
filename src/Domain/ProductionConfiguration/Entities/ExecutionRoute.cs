using Domain.Common;
using Domain.Catalog.Entities;
using System.Text.Json;

namespace Domain.ProductionConfiguration.Entities;

public class ExecutionRoute : BusinessEntity
{
    private readonly List<ExecutionRouteRobotBinding> _robotBindings = [];

    public Guid ConfigurationReleaseId { get; private set; }

    public Guid ProductVariantId { get; private set; }

    public Guid RecipeId { get; private set; }

    public string RouteCode { get; private set; } = null!;

    public int Priority { get; private set; }

    public string? RequiredCapabilitiesJson { get; private set; }

    public string SupportedOptionCodesJson { get; private set; } = "[]";

    public int? ProductionDefinitionSchemaVersion { get; private set; }

    public string? ProductionDefinitionJson { get; private set; }

    public string? ProductionDefinitionChecksum { get; private set; }

    public virtual ConfigurationRelease ConfigurationRelease { get; private set; } = null!;

    public virtual ProductVariant ProductVariant { get; private set; } = null!;

    public virtual Recipe Recipe { get; private set; } = null!;

    public IReadOnlyCollection<ExecutionRouteRobotBinding> RobotBindings => _robotBindings;

    public bool HasRobotBindings => _robotBindings.Any();

    private ExecutionRoute()
    {
    }

    internal static ExecutionRoute Create(
        Guid productVariantId,
        Guid recipeId,
        string routeCode,
        int priority,
        string? requiredCapabilitiesJson,
        IReadOnlyCollection<string> supportedOptionCodes)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new DomainRuleException("Product variant id is required.");
        }

        if (recipeId == Guid.Empty)
        {
            throw new DomainRuleException("Recipe id is required.");
        }

        if (string.IsNullOrWhiteSpace(routeCode))
        {
            throw new DomainRuleException("Execution route code is required.");
        }

        if (priority < 0)
        {
            throw new DomainRuleException("Execution route priority cannot be negative.");
        }

        var normalizedOptionCodes = supportedOptionCodes
            .Select(code => code?.Trim().ToUpperInvariant())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (normalizedOptionCodes.Length != supportedOptionCodes.Count)
        {
            throw new DomainRuleException("Execution route supported option codes must be non-empty and unique.");
        }

        return new ExecutionRoute
        {
            ProductVariantId = productVariantId,
            RecipeId = recipeId,
            RouteCode = routeCode.Trim(),
            Priority = priority,
            RequiredCapabilitiesJson = requiredCapabilitiesJson,
            SupportedOptionCodesJson = JsonSerializer.Serialize(normalizedOptionCodes)
        };
    }

    public IReadOnlyCollection<string> GetSupportedOptionCodes() =>
        JsonSerializer.Deserialize<string[]>(SupportedOptionCodesJson) ?? [];

    internal ExecutionRouteRobotBinding AddRobotBinding(
        Guid? productionProgramBindingId,
        string? productionProgramBindingChecksum,
        Guid robotProgramId,
        int bindingOrder,
        string requiredWorkcellCapabilityCode)
    {
        if (_robotBindings.Any(binding => binding.BindingOrder == bindingOrder))
        {
            throw new DomainRuleException("An execution route robot binding with the same order already exists.");
        }

        var binding = ExecutionRouteRobotBinding.Create(
            productionProgramBindingId,
            productionProgramBindingChecksum,
            robotProgramId,
            bindingOrder,
            requiredWorkcellCapabilityCode);
        _robotBindings.Add(binding);
        return binding;
    }

    internal ExecutionRouteRobotBinding AddRobotBinding(
        Guid robotProgramId,
        int bindingOrder,
        string requiredWorkcellCapabilityCode) =>
        AddRobotBinding(null, null, robotProgramId, bindingOrder, requiredWorkcellCapabilityCode);

    public void SetPublishedProductionDefinition(int schemaVersion, string definitionJson, string checksum)
    {
        if (schemaVersion <= 0 || string.IsNullOrWhiteSpace(definitionJson) || string.IsNullOrWhiteSpace(checksum))
            throw new DomainRuleException("Production definition schema, JSON, and checksum are required.");
        ProductionDefinitionSchemaVersion = schemaVersion;
        ProductionDefinitionJson = definitionJson;
        ProductionDefinitionChecksum = checksum.Trim();
    }
}
