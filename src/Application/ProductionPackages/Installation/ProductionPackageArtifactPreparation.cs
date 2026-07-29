using Application.RobotConfiguration.ArtifactContracts;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Storage.Services;
using Domain.Common;
using Domain.ProductionPackages;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.ArtifactTemplates;
using Domain.RobotConfiguration.Artifacts;

namespace Application.ProductionPackages.Installation;

public sealed record PreparedPackageArtifacts(
    IReadOnlyDictionary<string, RobotArtifact> BySourceKey);

public static class ProductionPackageArtifactPreparation
{
    public static async Task<PreparedPackageArtifacts> PrepareAsync(
        Guid organizationId,
        Guid actorId,
        IReadOnlyCollection<ProductionPackageArtifactDefinition> artifactDefinitions,
        IReadOnlyCollection<RobotArtifactTemplate> templates,
        IReadOnlyCollection<RobotArtifactTechnicalContract> contracts,
        IReadOnlyCollection<RobotArtifact> existingArtifacts,
        IReadOnlySet<Guid> packageManagedArtifactIds,
        IArtifactObjectStorage objectStorage,
        ArtifactPublicationValidator publicationValidator,
        UncommittedArtifactObjectSet preparedObjects,
        CancellationToken cancellationToken)
    {
        var templatesById = templates.ToDictionary(template => template.Id);
        var contractsById = contracts.ToDictionary(contract => contract.Id);
        var prepared = new Dictionary<string, RobotArtifact>(StringComparer.Ordinal);
        foreach (var definition in artifactDefinitions)
        {
            if (!templatesById.TryGetValue(definition.RobotArtifactTemplateId, out var template) ||
                !contractsById.TryGetValue(definition.TechnicalContractId, out var contract))
                throw new DomainRuleException("Package artifact source no longer exists.");

            var existing = existingArtifacts.SingleOrDefault(artifact =>
                artifact.ArtifactCode == definition.SourceKey && artifact.Checksum == template.Checksum);
            if (existing is not null)
            {
                ValidateReusableArtifact(definition, template, contract, existing, packageManagedArtifactIds);
                await publicationValidator.ValidateAsync(existing, cancellationToken);
                prepared.Add(definition.SourceKey, existing);
                continue;
            }

            var artifactId = Guid.NewGuid();
            var destination = $"robot-artifacts/{organizationId:D}/{artifactId:D}/{template.Checksum}.lua";
            preparedObjects.Track(destination);
            await objectStorage.CopyImmutableAsync(template.StorageKey,
                new ArtifactObjectWriteRequest(destination, "application/octet-stream", template.ContentLengthBytes,
                    template.Checksum), cancellationToken);
            var artifact = RobotArtifact.CreateDraft(organizationId, definition.SourceKey,
                template.TemplateName, destination, template.FileName, template.Checksum, template.RuntimeTargetCode,
                template.MachineModelCode, template.ContentLengthBytes, template.ExportedAt, template.Description,
                template.MetadataJson, template.Id, contract.Id, contract.ContractChecksum);
            artifact.Id = artifactId;
            artifact.CreatedByAccountId = actorId;
            prepared.Add(definition.SourceKey, artifact);
        }
        return new PreparedPackageArtifacts(prepared);
    }

    public static void ValidateReusableArtifact(
        ProductionPackageArtifactDefinition definition,
        RobotArtifactTemplate template,
        RobotArtifactTechnicalContract contract,
        RobotArtifact existing,
        IReadOnlySet<Guid> packageManagedArtifactIds)
    {
        if (!packageManagedArtifactIds.Contains(existing.Id))
            throw new DomainRuleException(
                $"Package artifact {definition.SourceKey} conflicts with an organization-authored artifact. Rename or discard the existing artifact before installing the package.");
        if (existing.Status == RobotArtifactStatus.Retired)
            throw new DomainRuleException($"Package artifact {definition.SourceKey} already exists but is Retired.");
        if (existing.SourceRobotArtifactTemplateId != template.Id ||
            existing.TechnicalContractId != contract.Id ||
            !string.Equals(existing.TechnicalContractChecksum, contract.ContractChecksum, StringComparison.Ordinal) ||
            !string.Equals(existing.RuntimeTargetCode, template.RuntimeTargetCode, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(existing.MachineModelCode, template.MachineModelCode, StringComparison.OrdinalIgnoreCase) ||
            existing.ContentLengthBytes != template.ContentLengthBytes)
            throw new DomainRuleException(
                $"Package artifact {definition.SourceKey} conflicts with an existing organization artifact identity.");
    }
}
