using Application.ProductionConfiguration.Bindings;
using Domain.Catalog.Entities;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.Programs;
using Domain.RobotConfiguration.Programs.Manifests;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Infrastructure.ProductionConfiguration.Persistence.Bindings;

public sealed class ProductionProgramBindingStore(IceBotDbContext db) : IProductionProgramBindingStore
{
    public Task<Recipe?> GetRecipeAsync(Guid recipeId, CancellationToken cancellationToken) =>
        db.Recipes.Include(recipe => recipe.ProductVariant).ThenInclude(variant => variant.Product)
            .ThenInclude(product => product.OptionGroups).ThenInclude(group => group.ProductOptions)
            .SingleOrDefaultAsync(recipe => recipe.Id == recipeId && recipe.DeletedAt == null, cancellationToken);

    public Task<RobotProgram?> GetRobotProgramAsync(Guid robotProgramId, CancellationToken cancellationToken) =>
        db.RobotPrograms.SingleOrDefaultAsync(program => program.Id == robotProgramId && program.DeletedAt == null, cancellationToken);

    public async Task<ProductionProgramCapabilityProposal> GetProgramCapabilityProposalAsync(
        RobotProgram program, CancellationToken cancellationToken)
    {
        var document = RobotProgramManifestBuilder.Parse(program.ProgramManifestJson!);
        var references = document.Artifacts
            .Select(item => item.RobotArtifact)
            .Where(item => item.TechnicalContractId.HasValue && !string.IsNullOrWhiteSpace(item.TechnicalContractChecksum))
            .Select(item => new { Id = item.TechnicalContractId!.Value, Checksum = item.TechnicalContractChecksum!.Trim().ToLowerInvariant() })
            .Distinct()
            .ToArray();
        var codes = Array.Empty<string>();
        if (references.Length > 0)
        {
            var contractIds = references.Select(reference => reference.Id).ToArray();
            var contracts = await db.RobotArtifactTechnicalContracts
                .Include(contract => contract.Effects)
                .Where(contract => contractIds.Contains(contract.Id) && contract.DeletedAt == null)
                .ToListAsync(cancellationToken);
            if (contracts.Count != contractIds.Length || contracts.Any(contract =>
                    contract.Status != RobotArtifactContractStatus.Published ||
                    (contract.OrganizationId.HasValue && contract.OrganizationId != program.OrganizationId) ||
                    !references.Any(reference => reference.Id == contract.Id &&
                        string.Equals(reference.Checksum, contract.ContractChecksum, StringComparison.OrdinalIgnoreCase))))
            {
                throw new DomainRuleException(
                    "A technical declaration referenced by the published program is missing, out of scope, unpublished, or checksum-inconsistent.");
            }

            codes = contracts.SelectMany(contract => contract.Effects)
                .Select(effect => effect.RequiredWorkcellCapabilityCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code!.Trim().ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
        }

        return RobotProgramCapabilityProfileDefaults.Resolve(document, codes);
    }

    public async Task<ProductionProgramBinding?> FindActiveEquivalentAsync(Guid organizationId, Guid recipeId, Guid robotProgramId,
        IReadOnlyCollection<string> requiredCapabilityCodes,
        ProductionProgramBindingCapabilityEvidenceStatus capabilityEvidenceStatus,
        ProductionProgramBindingAssurance assurance,
        IReadOnlyCollection<string> supportedOptionCodes, CancellationToken cancellationToken)
    {
        var capabilityJson = JsonSerializer.Serialize(requiredCapabilityCodes.Select(code => code.Trim().ToUpperInvariant())
            .Order(StringComparer.Ordinal).ToArray());
        var requestedJson = JsonSerializer.Serialize(supportedOptionCodes.Select(code => code.Trim().ToUpperInvariant())
            .Order(StringComparer.Ordinal).ToArray());
        return await db.ProductionProgramBindings.SingleOrDefaultAsync(binding =>
            binding.OrganizationId == organizationId && binding.RecipeId == recipeId && binding.RobotProgramId == robotProgramId &&
            binding.RequiredCapabilityCodesJson == capabilityJson && binding.CapabilityEvidenceStatus == capabilityEvidenceStatus &&
            binding.Assurance == assurance &&
            binding.SupportedOptionCodesJson == requestedJson &&
            binding.Status == ProductionProgramBindingStatus.Active && binding.DeletedAt == null, cancellationToken);
    }

    public Task<ProductionProgramBinding?> GetAsync(Guid organizationId, Guid bindingId, CancellationToken cancellationToken) =>
        db.ProductionProgramBindings.SingleOrDefaultAsync(binding => binding.Id == bindingId && binding.OrganizationId == organizationId && binding.DeletedAt == null, cancellationToken);

    public async Task<IReadOnlyList<ProductionProgramBinding>> ListAsync(Guid organizationId, ProductionProgramBindingStatus? status,
        CancellationToken cancellationToken) =>
        await db.ProductionProgramBindings.AsNoTracking().Where(binding => binding.OrganizationId == organizationId && binding.DeletedAt == null &&
            (!status.HasValue || binding.Status == status.Value)).OrderByDescending(binding => binding.CreatedAt).ToListAsync(cancellationToken);

    public Task AddAsync(ProductionProgramBinding binding, CancellationToken cancellationToken)
    {
        db.ProductionProgramBindings.Add(binding);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
