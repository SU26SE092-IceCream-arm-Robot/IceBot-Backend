using System.Text.Json;

namespace Application.ProductionConfiguration.Routes.Contracts;

public sealed record ExecutionRouteCapabilityRequirementContract(
    string Code,
    bool Required);

public static class ExecutionRouteCapabilityRequirementContractCodec
{
    public static string? ToStorageJson(IReadOnlyCollection<ExecutionRouteCapabilityRequirementContract> requirements)
    {
        if (requirements.Count == 0)
        {
            return null;
        }

        var normalized = requirements
            .Select(requirement => new
            {
                Code = requirement.Code?.Trim().ToUpperInvariant(),
                requirement.Required
            })
            .OrderBy(requirement => requirement.Code, StringComparer.Ordinal)
            .ToArray();

        return JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            requires = normalized.Select(requirement => new
            {
                code = requirement.Code,
                required = requirement.Required
            })
        });
    }
}
