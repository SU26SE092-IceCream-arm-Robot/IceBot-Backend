namespace Application.Devices.Support;

public static class DeviceModelRequestValidator
{
    private const int MaxCapabilityCount = 100;
    private const int MaxCapabilityLength = 100;

    public static string? ValidateCapabilities(IReadOnlyList<string>? capabilities)
    {
        if (capabilities is null)
        {
            return null;
        }

        if (capabilities.Count > MaxCapabilityCount)
        {
            return $"A device model can define at most {MaxCapabilityCount} capabilities.";
        }

        if (capabilities.Any(string.IsNullOrWhiteSpace))
        {
            return "Capabilities cannot contain empty values.";
        }

        if (capabilities.Any(x => x.Trim().Length > MaxCapabilityLength))
        {
            return $"Each capability must be at most {MaxCapabilityLength} characters.";
        }

        if (capabilities.Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != capabilities.Count)
        {
            return "Capabilities cannot contain duplicate values.";
        }

        return null;
    }
}
