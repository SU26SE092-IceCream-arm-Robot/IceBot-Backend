using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.AuthoringImports;
using Domain.RobotConfiguration.Programs;

namespace Application.RobotConfiguration.AuthoringImports;

public sealed class RobotAuthoringImportValidator(IRobotAuthoringImportStore store)
{
    public async Task<RobotAuthoringImportValidationReport> BuildReportAsync(
        RobotAuthoringImport session, RobotAuthoringBundle bundle, CancellationToken cancellationToken)
    {
        var errors = new List<RobotAuthoringImportValidationIssue>();
        var warnings = new List<RobotAuthoringImportValidationIssue>();
        var codes = bundle.Items.Select(x => Normalize(x.ManifestItem.ArtifactCode)).ToArray();
        var artifacts = await store.GetArtifactsAsync(session.OrganizationId, codes, false, cancellationToken);
        foreach (var item in bundle.Items)
        {
            var code = Normalize(item.ManifestItem.ArtifactCode);
            var matchingArtifacts = artifacts.Where(x => x.ArtifactCode == code).ToArray();
            var artifact = matchingArtifacts.Length == 1 ? matchingArtifacts[0] : null;
            if (matchingArtifacts.Length > 1)
                errors.Add(new("ARTIFACT_REVISION_AMBIGUOUS",
                    "Multiple artifact revisions use this code; choose an explicit revision code.", code));
            if (artifact is not null && artifact.Checksum != item.LuaChecksum)
                errors.Add(new("ARTIFACT_REVISION_CONFLICT",
                    "Artifact code already exists with different bytes; choose a new revision code.", code));
            if (artifact is not null &&
                (!string.Equals(artifact.RuntimeTargetCode, session.RuntimeTargetCode, StringComparison.OrdinalIgnoreCase) ||
                 !string.Equals(artifact.MachineModelCode, session.MachineModelCode, StringComparison.OrdinalIgnoreCase)))
                errors.Add(new("ARTIFACT_RUNTIME_PROFILE_CONFLICT",
                    "Artifact code already exists with a different runtime target or machine model; choose a new revision code.", code));
            if (artifact?.Status == RobotArtifactStatus.Retired)
                errors.Add(new("ARTIFACT_NOT_REUSABLE",
                    "A retired robot artifact cannot be reused by a new authoring import.", code));
        }

        var program = await store.GetProgramAsync(session.OrganizationId, session.StoreId, session.KioskId,
            session.DeviceId, session.ProposedProgramCode, false, cancellationToken);
        if (program is not null)
        {
            var artifactByCode = artifacts
                .GroupBy(x => x.ArtifactCode, StringComparer.Ordinal)
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
            var expected = bundle.Items.OrderBy(x => x.ManifestItem.RunOrder)
                .Select(x => artifactByCode.TryGetValue(Normalize(x.ManifestItem.ArtifactCode), out var artifact)
                    ? artifact.Id : Guid.Empty)
                .ToArray();
            var actual = program.RobotProgramArtifacts.OrderBy(x => x.RunOrder)
                .Select(x => x.RobotArtifactId).ToArray();
            if (program.Status != RobotProgramStatus.Draft || expected.Contains(Guid.Empty) ||
                !actual.SequenceEqual(expected))
                errors.Add(new("PROGRAM_IDENTITY_CONFLICT",
                    "Program code already exists with a different manifest or lifecycle state."));
        }

        var existingArtifactCount = bundle.Items.Count(item =>
            artifacts.Count(artifact => artifact.ArtifactCode == Normalize(item.ManifestItem.ArtifactCode)) == 1);
        return new RobotAuthoringImportValidationReport(errors.Count == 0, errors, warnings,
            existingArtifactCount, bundle.Items.Count - existingArtifactCount,
            0, 0);
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
