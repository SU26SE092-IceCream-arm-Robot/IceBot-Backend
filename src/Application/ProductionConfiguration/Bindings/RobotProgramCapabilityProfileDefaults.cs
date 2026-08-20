using Domain.ProductionConfiguration.Entities;
using Domain.RobotConfiguration.Programs.Manifests;

namespace Application.ProductionConfiguration.Bindings;

// Packaging-profile defaults are routing declarations, never proof of Lua behavior or installed hardware.
public static class RobotProgramCapabilityProfileDefaults
{
    public const string FairinoLuaV1RuntimeTarget = "FAIRINO_LUA_V1";
    public const string FairinoFr5MachineModel = "FR5";
    public const string RobotArmCapability = "ROBOT_ARM";

    public static ProductionProgramCapabilityProposal Resolve(
        RobotProgramManifestDocument document,
        IReadOnlyCollection<string> declaredCapabilityCodes)
    {
        if (declaredCapabilityCodes.Count > 0)
            return new ProductionProgramCapabilityProposal(declaredCapabilityCodes,
                ProductionProgramBindingCapabilityEvidenceStatus.Declared);

        if (document.Artifacts.Count > 0 && document.Artifacts.All(IsFairinoFr5Artifact))
            return new ProductionProgramCapabilityProposal([RobotArmCapability],
                ProductionProgramBindingCapabilityEvidenceStatus.TargetProfileDefault);

        return new ProductionProgramCapabilityProposal([], ProductionProgramBindingCapabilityEvidenceStatus.Missing);
    }

    private static bool IsFairinoFr5Artifact(RobotProgramManifestItem item) =>
        string.Equals(item.RobotArtifact.RuntimeTargetCode, FairinoLuaV1RuntimeTarget, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(item.RobotArtifact.MachineModelCode, FairinoFr5MachineModel, StringComparison.OrdinalIgnoreCase);
}
