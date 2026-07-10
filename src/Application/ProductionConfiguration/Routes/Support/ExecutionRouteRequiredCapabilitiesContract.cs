using System.Text.Json;

namespace Application.ProductionConfiguration.Routes.Support;

public static class ExecutionRouteRequiredCapabilitiesContract
{
    private const int SchemaVersion = 1;
    private const int MaxJsonLength = 2_000;
    private const int MaxRequirementCount = 50;
    private const int MaxCapabilityCodeLength = 100;
    private const int MaxVersionLength = 50;

    private static readonly HashSet<string> RootProperties = new(StringComparer.Ordinal)
    {
        "schemaVersion",
        "requires"
    };

    private static readonly HashSet<string> RequirementProperties = new(StringComparer.Ordinal)
    {
        "code",
        "minVersion",
        "required"
    };

    public static string? Validate(string? value, IReadOnlyCollection<string> allowedCapabilityCodes)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Length > MaxJsonLength)
        {
            return $"Execution route required capabilities JSON must be at most {MaxJsonLength} characters.";
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(value);
        }
        catch (JsonException)
        {
            return "Execution route required capabilities must be valid JSON.";
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return "Execution route required capabilities must be a JSON object.";
            }

            foreach (var property in root.EnumerateObject())
            {
                if (!RootProperties.Contains(property.Name))
                {
                    return $"Execution route required capabilities contains unsupported field '{property.Name}'.";
                }
            }

            if (!root.TryGetProperty("schemaVersion", out var schemaVersionElement) ||
                schemaVersionElement.ValueKind != JsonValueKind.Number ||
                !schemaVersionElement.TryGetInt32(out var schemaVersion) ||
                schemaVersion != SchemaVersion)
            {
                return $"Execution route required capabilities schemaVersion must be {SchemaVersion}.";
            }

            if (!root.TryGetProperty("requires", out var requiresElement) ||
                requiresElement.ValueKind != JsonValueKind.Array)
            {
                return "Execution route required capabilities requires must be an array.";
            }

            var requirements = requiresElement.EnumerateArray().ToArray();
            if (requirements.Length == 0)
            {
                return "Execution route required capabilities requires must contain at least one item.";
            }

            if (requirements.Length > MaxRequirementCount)
            {
                return $"Execution route required capabilities can contain at most {MaxRequirementCount} items.";
            }

            var allowedCodes = allowedCapabilityCodes
                .Select(code => code.Trim().ToUpperInvariant())
                .Where(code => code.Length > 0)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var requirement in requirements)
            {
                var error = ValidateRequirement(requirement, allowedCodes, seenCodes);
                if (error is not null)
                {
                    return error;
                }
            }
        }

        return null;
    }

    private static string? ValidateRequirement(
        JsonElement requirement,
        IReadOnlySet<string> allowedCodes,
        ISet<string> seenCodes)
    {
        if (requirement.ValueKind != JsonValueKind.Object)
        {
            return "Each execution route required capability must be a JSON object.";
        }

        foreach (var property in requirement.EnumerateObject())
        {
            if (!RequirementProperties.Contains(property.Name))
            {
                return $"Execution route required capability contains unsupported field '{property.Name}'.";
            }
        }

        if (!requirement.TryGetProperty("code", out var codeElement) ||
            codeElement.ValueKind != JsonValueKind.String)
        {
            return "Each execution route required capability requires a code.";
        }

        var code = codeElement.GetString()?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
        {
            return "Execution route required capability code cannot be empty.";
        }

        if (code.Length > MaxCapabilityCodeLength)
        {
            return $"Execution route required capability code must be at most {MaxCapabilityCodeLength} characters.";
        }

        if (!allowedCodes.Contains(code))
        {
            return $"Execution route required capability code '{code}' is not declared by a route robot binding.";
        }

        if (!seenCodes.Add(code))
        {
            return $"Execution route required capability code '{code}' is duplicated.";
        }

        if (requirement.TryGetProperty("minVersion", out var minVersionElement))
        {
            if (minVersionElement.ValueKind != JsonValueKind.String)
            {
                return "Execution route required capability minVersion must be a string.";
            }

            var minVersion = minVersionElement.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(minVersion) || minVersion.Length > MaxVersionLength)
            {
                return $"Execution route required capability minVersion must be non-empty and at most {MaxVersionLength} characters.";
            }
        }

        if (requirement.TryGetProperty("required", out var requiredElement) &&
            requiredElement.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            return "Execution route required capability required must be a boolean.";
        }

        return null;
    }
}
