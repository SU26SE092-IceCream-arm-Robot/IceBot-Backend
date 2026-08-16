using Domain.Common;
using Domain.Devices.ExecutionEndpoints;

namespace Application.ProductionConfiguration.Deployments.Services;

public static class DeploymentRuntimeCompatibilityRules
{
    public static IReadOnlyCollection<(string RuntimeTargetCode, string MachineModelCode)> FindMismatches(
        IEnumerable<(string RuntimeTargetCode, string MachineModelCode)> requiredProfiles,
        IReadOnlyCollection<ExecutionEndpointReportedDevice> reportedDevices)
    {
        if (reportedDevices.Count == 0)
            return [];

        return requiredProfiles
            .Distinct(RuntimeProfileComparer.Instance)
            .Where(required => !reportedDevices.Any(reported =>
                string.Equals(required.RuntimeTargetCode, reported.RuntimeTargetCode,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(required.MachineModelCode, reported.MachineModelCode,
                    StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    public static void EnsureCompatibleWhenReported(
        IEnumerable<(string RuntimeTargetCode, string MachineModelCode)> requiredProfiles,
        IReadOnlyCollection<ExecutionEndpointReportedDevice> reportedDevices)
    {
        var mismatches = FindMismatches(requiredProfiles, reportedDevices);
        if (mismatches.Count == 0)
            return;

        throw new DomainRuleException(
            $"Execution endpoint does not report the required runtime profiles: {Format(mismatches)}.");
    }

    public static string Format(
        IEnumerable<(string RuntimeTargetCode, string MachineModelCode)> profiles) =>
        string.Join(", ", profiles.Select(profile =>
            $"{profile.RuntimeTargetCode}/{profile.MachineModelCode}"));

    private sealed class RuntimeProfileComparer : IEqualityComparer<(string RuntimeTargetCode, string MachineModelCode)>
    {
        public static RuntimeProfileComparer Instance { get; } = new();

        public bool Equals(
            (string RuntimeTargetCode, string MachineModelCode) x,
            (string RuntimeTargetCode, string MachineModelCode) y) =>
            string.Equals(x.RuntimeTargetCode, y.RuntimeTargetCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.MachineModelCode, y.MachineModelCode, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string RuntimeTargetCode, string MachineModelCode) value) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(value.RuntimeTargetCode),
                StringComparer.OrdinalIgnoreCase.GetHashCode(value.MachineModelCode));
    }
}
