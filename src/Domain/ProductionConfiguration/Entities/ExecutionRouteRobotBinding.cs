using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Domain.Common;
using Domain.RobotConfiguration.Artifacts;
using System.Text.Json;

namespace Domain.ProductionConfiguration.Entities;

public class ExecutionRouteRobotBinding : BusinessEntity
{
    public Guid ExecutionRouteId { get; private set; }

    public int BindingOrder { get; private set; }

    public string RequiredCapabilityCodesJson { get; private set; } = "[]";

    public Guid RobotProgramId { get; private set; }

    public Guid ProductionProgramBindingId { get; private set; }

    public string ProductionProgramBindingChecksum { get; private set; } = null!;

    public virtual ExecutionRoute ExecutionRoute { get; private set; } = null!;

    public virtual RobotProgram RobotProgram { get; private set; } = null!;

    private ExecutionRouteRobotBinding()
    {
    }

    internal static ExecutionRouteRobotBinding Create(
        Guid productionProgramBindingId,
        string productionProgramBindingChecksum,
        Guid robotProgramId,
        int bindingOrder,
        IReadOnlyCollection<string> requiredCapabilityCodes)
    {
        if (productionProgramBindingId == Guid.Empty || robotProgramId == Guid.Empty)
        {
            throw new DomainRuleException("Production binding and robot program ids are required.");
        }

        if (bindingOrder <= 0)
        {
            throw new DomainRuleException("Execution route robot binding order must be greater than zero.");
        }

        var normalizedCapabilityCodes = NormalizeCapabilityCodes(requiredCapabilityCodes);

        return new ExecutionRouteRobotBinding
        {
            ProductionProgramBindingId = productionProgramBindingId,
            ProductionProgramBindingChecksum = RequireChecksum(productionProgramBindingChecksum),
            RobotProgramId = robotProgramId,
            BindingOrder = bindingOrder,
            RequiredCapabilityCodesJson = JsonSerializer.Serialize(normalizedCapabilityCodes)
        };
    }

    public IReadOnlyCollection<string> GetRequiredCapabilityCodes() =>
        JsonSerializer.Deserialize<string[]>(RequiredCapabilityCodesJson) ?? [];

    private static string[] NormalizeCapabilityCodes(IReadOnlyCollection<string> codes) =>
        codes.Select(code =>
            string.IsNullOrWhiteSpace(code)
                ? throw new DomainRuleException("Required capability code cannot be empty.")
                : code.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string RequireChecksum(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
            throw new DomainRuleException("Production binding checksum must be a SHA-256 checksum.");
        return normalized;
    }
}
