using Domain.Common;
using Domain.RobotConfiguration.Programs;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.ProductionConfiguration.ValueObjects;

namespace Application.ProductionConfiguration.Releases.Services;

public static class PublishedRobotProgramSnapshotFactory
{
    public static IReadOnlyDictionary<Guid, PublishedRobotProgramSnapshot> CreateForPublication(
        Domain.ProductionConfiguration.Entities.ConfigurationRelease release) =>
        CreateForRelease(release, allowRetiredPrograms: false);

    public static IReadOnlyDictionary<Guid, PublishedRobotProgramSnapshot> CreateForDeployment(
        Domain.ProductionConfiguration.Entities.ConfigurationRelease release) =>
        CreateForRelease(release, allowRetiredPrograms: true);

    private static IReadOnlyDictionary<Guid, PublishedRobotProgramSnapshot> CreateForRelease(
        Domain.ProductionConfiguration.Entities.ConfigurationRelease release,
        bool allowRetiredPrograms) =>
        release.ExecutionRoutes
            .SelectMany(route => route.RobotBindings)
            .Select(binding => binding.RobotProgram)
            .DistinctBy(program => program.Id)
            .ToDictionary(
                program => program.Id,
                program => Create(program, release.OrganizationId, allowRetiredPrograms));

    private static PublishedRobotProgramSnapshot Create(
        RobotProgram program,
        Guid organizationId,
        bool allowRetiredPrograms)
    {
        var validStatus = program.Status == RobotProgramStatus.Published ||
            (allowRetiredPrograms && program.Status == RobotProgramStatus.Retired);
        if (!validStatus ||
            program.OrganizationId != organizationId ||
            string.IsNullOrWhiteSpace(program.ProgramManifestChecksum) ||
            string.IsNullOrWhiteSpace(program.ProgramManifestJson))
        {
            throw new DomainRuleException(
                "Configuration release requires published organization-owned robot programs; deployment may reuse retired immutable programs.");
        }

        var manifest = RobotProgramManifestBuilder.Parse(program.ProgramManifestJson);
        if (manifest.Id != program.Id || manifest.SchemaVersion != program.ProgramManifestSchemaVersion)
        {
            throw new DomainRuleException(
                "Robot program manifest identity does not match the published program.");
        }

        return new PublishedRobotProgramSnapshot(
            program.Id,
            program.Code,
            organizationId,
            program.ProgramManifestSchemaVersion,
            program.ProgramManifestChecksum,
            manifest.Artifacts.Select(item => new PublishedRobotArtifactSnapshot(
                item.Id,
                item.RobotArtifact.Id,
                item.RunOrder,
                item.ParametersSchemaVersion,
                item.Parameters?.ToJsonString(),
                item.RobotArtifact.Checksum,
                item.RobotArtifact.StorageKey,
                item.RobotArtifact.RuntimeTargetCode,
                item.RobotArtifact.MachineModelCode,
                item.RobotArtifact.ContentLengthBytes,
                item.RobotArtifact.TechnicalContractId,
                item.RobotArtifact.TechnicalContractChecksum,
                item.RequiredOptionCode)).ToArray(),
            manifest.RestartPolicy);
    }
}
