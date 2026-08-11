using Domain.RobotConfiguration.ArtifactContracts;
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
        var contracts = await store.GetContractsAsync(session.OrganizationId, codes, false, cancellationToken);
        var artifacts = await store.GetArtifactsAsync(session.OrganizationId, codes, false, cancellationToken);
        foreach (var item in bundle.Items)
        {
            var code = Normalize(item.ManifestItem.ArtifactCode);
            var matchingContracts = contracts.Where(x => x.ContractCode == code).ToArray();
            var contract = matchingContracts.Length == 1 ? matchingContracts[0] : null;
            if (matchingContracts.Length > 1)
                warnings.Add(new("TECHNICAL_DECLARATION_IDENTITY_CONFLICT",
                    "Multiple technical declarations use this code; none will be attached automatically.", code));
            if (contract is not null && !DeclarationMatches(contract, item.Sidecar))
                warnings.Add(new("TECHNICAL_DECLARATION_CONFLICT",
                    "The existing declaration differs from the uploaded metadata; it will not be attached automatically.", code));
            if (contract?.Status == RobotArtifactContractStatus.Retired)
                warnings.Add(new("TECHNICAL_DECLARATION_NOT_REUSABLE",
                    "The retired declaration will not be attached automatically.", code));

            var matchingArtifacts = artifacts.Where(x => x.ArtifactCode == code).ToArray();
            var artifact = matchingArtifacts.Length == 1 ? matchingArtifacts[0] : null;
            if (matchingArtifacts.Length > 1)
                errors.Add(new("ARTIFACT_REVISION_AMBIGUOUS",
                    "Multiple artifact revisions use this code; choose an explicit revision code.", code));
            if (artifact is not null && artifact.Checksum != item.LuaChecksum)
                errors.Add(new("ARTIFACT_REVISION_CONFLICT",
                    "Artifact code already exists with different bytes; choose a new revision code.", code));
            if (artifact?.Status == RobotArtifactStatus.Retired)
                errors.Add(new("ARTIFACT_NOT_REUSABLE",
                    "A retired robot artifact cannot be reused by a new authoring import.", code));
            if (item.Sidecar.Effects.Count == 0 || item.Sidecar.Effects.All(x =>
                    x.EffectKind is RobotArtifactEffectKind.System or RobotArtifactEffectKind.Motion))
                warnings.Add(new("NO_PRODUCTION_DECLARATIONS",
                    "No ingredient or option declarations were supplied. Recipe selection remains an operator decision.", code));
        }

        ValidateDeclaredOrder(bundle, warnings);
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
        var existingContractCount = bundle.Items.Count(item =>
            contracts.Count(contract => contract.ContractCode == Normalize(item.ManifestItem.ArtifactCode)) == 1);
        return new RobotAuthoringImportValidationReport(errors.Count == 0, errors, warnings,
            existingArtifactCount, bundle.Items.Count - existingArtifactCount,
            existingContractCount, bundle.Items.Count - existingContractCount);
    }

    private static void ValidateDeclaredOrder(RobotAuthoringBundle bundle,
        ICollection<RobotAuthoringImportValidationIssue> warnings)
    {
        var effectOwners = bundle.Items
            .SelectMany(item => item.Sidecar.Effects.Select(effect => new
            {
                Code = Normalize(effect.EffectCode),
                item.ManifestItem.ArtifactCode,
                item.ManifestItem.RunOrder
            }))
            .GroupBy(x => x.Code)
            .ToDictionary(x => x.Key, x => x.ToArray());
        foreach (var duplicate in effectOwners.Where(x => x.Value.Length > 1))
            warnings.Add(new("DECLARED_EFFECT_IDENTITY_AMBIGUOUS",
                $"Effect '{duplicate.Key}' is declared by multiple artifacts."));

        var previousPhaseRank = -1;
        foreach (var item in bundle.Items.OrderBy(x => x.ManifestItem.RunOrder))
        foreach (var constraint in item.Sidecar.OrderingConstraints)
        {
            if (constraint.ConstraintType == RobotArtifactOrderingConstraintType.Phase)
            {
                var rank = PhaseRank(constraint.Value);
                if (rank < 0)
                    warnings.Add(new("DECLARED_ORDERING_PHASE_UNKNOWN", $"Unknown declared ordering phase '{constraint.Value}'.",
                        item.ManifestItem.ArtifactCode));
                else if (rank < previousPhaseRank)
                    warnings.Add(new("DECLARED_ORDERING_PHASE_CONFLICT",
                        "Manifest RunOrder differs from the declared phase order; manifest order remains authoritative.", item.ManifestItem.ArtifactCode));
                else
                    previousPhaseRank = rank;
                continue;
            }

            var targetCode = Normalize(constraint.Value);
            if (!effectOwners.TryGetValue(targetCode, out var owners) || owners.Length != 1)
            {
                warnings.Add(new("DECLARED_ORDERING_EFFECT_UNRESOLVED",
                    $"Declared ordering target effect '{targetCode}' is missing or ambiguous.",
                    item.ManifestItem.ArtifactCode));
                continue;
            }
            var valid = constraint.ConstraintType == RobotArtifactOrderingConstraintType.BeforeEffect
                ? item.ManifestItem.RunOrder < owners[0].RunOrder
                : item.ManifestItem.RunOrder > owners[0].RunOrder;
            if (!valid)
                warnings.Add(new("DECLARED_ORDERING_CONFLICT",
                    $"Manifest RunOrder differs from declared {constraint.ConstraintType} '{targetCode}'; manifest order remains authoritative.",
                    item.ManifestItem.ArtifactCode));
        }
    }

    internal static bool DeclarationMatches(RobotArtifactTechnicalContract contract, RobotAuthoringSidecar sidecar) =>
        contract.SchemaVersion == sidecar.SchemaVersion &&
        EqualsCode(contract.RuntimeTargetCode, sidecar.RuntimeTargetCode) &&
        EqualsCode(contract.MachineModelCode, sidecar.MachineModelCode) &&
        contract.Effects.OrderBy(x => x.EffectCode).Select(x => (x.EffectCode, x.EffectKind, x.IngredientCode,
            x.OptionCode, x.QuantityMode, x.FixedQuantity, x.Unit, x.RequiredWorkcellCapabilityCode))
        .SequenceEqual(sidecar.Effects.OrderBy(x => Normalize(x.EffectCode)).Select(x => (Normalize(x.EffectCode),
            x.EffectKind, NormalizeOptional(x.IngredientCode), NormalizeOptional(x.OptionCode), x.QuantityMode,
            x.FixedQuantity, x.Unit?.Trim().ToLowerInvariant(), NormalizeOptional(x.RequiredWorkcellCapabilityCode)))) &&
        contract.OrderingConstraints.OrderBy(x => x.ConstraintType).ThenBy(x => x.SortHint).ThenBy(x => x.Value)
        .Select(x => (x.ConstraintType, x.Value, x.SortHint))
        .SequenceEqual(sidecar.OrderingConstraints.OrderBy(x => x.ConstraintType).ThenBy(x => x.SortHint)
            .ThenBy(x => Normalize(x.Value)).Select(x => (x.ConstraintType, Normalize(x.Value), x.SortHint)));

    private static int PhaseRank(string value) => Normalize(value) switch
    {
        "PREPARE" => 0,
        "BASE" => 1,
        "OPTION" => 2,
        "DELIVER" => 3,
        "CLEANUP" => 4,
        _ => -1
    };

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Normalize(value);
    private static bool EqualsCode(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
