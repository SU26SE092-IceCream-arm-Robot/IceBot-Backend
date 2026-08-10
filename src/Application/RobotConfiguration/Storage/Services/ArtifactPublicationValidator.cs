using System.Security.Cryptography;
using Application.RobotConfiguration.ArtifactContracts;
using Application.RobotConfiguration.Storage.Abstractions;
using Domain.Common;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.ArtifactTemplates;

namespace Application.RobotConfiguration.Storage.Services;

public sealed class ArtifactPublicationValidator(
    IRobotArtifactTechnicalContractStore technicalContracts,
    IArtifactObjectStorage objectStorage)
{
    public Task ValidateAsync(RobotArtifact artifact, CancellationToken cancellationToken = default) =>
        ValidateAsync(
            artifact.OrganizationId,
            artifact.TechnicalContractId,
            artifact.TechnicalContractChecksum,
            artifact.StorageKey,
            artifact.ContentLengthBytes,
            artifact.Checksum,
            cancellationToken);

    public Task ValidateAsync(RobotArtifactTemplate template, CancellationToken cancellationToken = default) =>
        ValidateAsync(
            organizationId: null,
            template.TechnicalContractId,
            template.TechnicalContractChecksum,
            template.StorageKey,
            template.ContentLengthBytes,
            template.Checksum,
            cancellationToken);

    private async Task ValidateAsync(
        Guid? organizationId,
        Guid? technicalContractId,
        string? technicalContractChecksum,
        string storageKey,
        long expectedLength,
        string expectedChecksum,
        CancellationToken cancellationToken)
    {
        if (technicalContractId.HasValue != !string.IsNullOrWhiteSpace(technicalContractChecksum))
        {
            throw new DomainRuleException(
                "Technical contract identity and declaration checksum must either both be assigned or both be absent.");
        }

        if (technicalContractId.HasValue)
        {
            var contract = await technicalContracts.GetAsync(technicalContractId.Value, false, cancellationToken);
            if (contract is null ||
                contract.Status != RobotArtifactContractStatus.Published ||
                string.IsNullOrWhiteSpace(contract.ContractChecksum) ||
                !string.Equals(contract.ContractChecksum, technicalContractChecksum, StringComparison.Ordinal) ||
                (contract.OrganizationId.HasValue && contract.OrganizationId != organizationId))
            {
                throw new DomainRuleException(
                    "The assigned technical declaration is not published, in scope, or checksum-consistent.");
            }
        }

        byte[] bytes;
        try
        {
            bytes = await objectStorage.ReadBytesAsync(storageKey, expectedLength, cancellationToken);
        }
        catch (ArtifactObjectSizeLimitExceededException)
        {
            throw new ArtifactObjectIntegrityException(
                storageKey,
                "The robot artifact object exceeds its declared content length.");
        }
        var actualChecksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (bytes.LongLength != expectedLength ||
            !string.Equals(actualChecksum, expectedChecksum, StringComparison.Ordinal))
        {
            throw new ArtifactObjectIntegrityException(
                storageKey,
                "The robot artifact object failed checksum or size verification before publication.");
        }
    }
}
