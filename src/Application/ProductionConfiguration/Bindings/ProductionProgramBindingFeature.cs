using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.RobotConfiguration.Programs;

namespace Application.ProductionConfiguration.Bindings;

public interface IProductionProgramBindingStore
{
    Task<Recipe?> GetRecipeAsync(Guid recipeId, CancellationToken cancellationToken);
    Task<RobotProgram?> GetRobotProgramAsync(Guid robotProgramId, CancellationToken cancellationToken);
    Task<ProductionProgramCapabilityProposal> GetProgramCapabilityProposalAsync(
        RobotProgram program, CancellationToken cancellationToken);
    Task<ProductionProgramBinding?> FindActiveEquivalentAsync(Guid organizationId, Guid recipeId, Guid robotProgramId,
        IReadOnlyCollection<string> requiredCapabilityCodes,
        ProductionProgramBindingCapabilityEvidenceStatus capabilityEvidenceStatus,
        ProductionProgramBindingAssurance assurance,
        IReadOnlyCollection<string> supportedOptionCodes,
        CancellationToken cancellationToken);
    Task<ProductionProgramBinding?> GetAsync(Guid organizationId, Guid bindingId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductionProgramBinding>> ListAsync(Guid organizationId, ProductionProgramBindingStatus? status,
        CancellationToken cancellationToken);
    Task AddAsync(ProductionProgramBinding binding, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record ProductionProgramCapabilityProposal(
    IReadOnlyCollection<string> DeclaredRequiredCapabilityCodes,
    ProductionProgramBindingCapabilityEvidenceStatus Status);

public sealed record CreateProductionProgramBindingCommand(
    CurrentUserContext UserContext,
    Guid OrganizationId,
    Guid RecipeId,
    Guid RobotProgramId,
    IReadOnlyCollection<string> SupportedOptionCodes);

public sealed record RetireProductionProgramBindingCommand(CurrentUserContext UserContext, Guid OrganizationId, Guid BindingId);

public sealed record ProductionProgramBindingResult(
    Guid Id,
    Guid OrganizationId,
    Guid ProductVariantId,
    Guid RecipeId,
    int RecipeVersion,
    Guid RobotProgramId,
    string ProgramManifestChecksum,
    IReadOnlyCollection<string> RequiredCapabilityCodes,
    string CapabilityEvidenceStatus,
    string Assurance,
    IReadOnlyCollection<string> SupportedOptionCodes,
    string BindingChecksum,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RetiredAt)
{
    public static ProductionProgramBindingResult From(ProductionProgramBinding binding) => new(
        binding.Id, binding.OrganizationId, binding.ProductVariantId, binding.RecipeId, binding.RecipeVersion,
        binding.RobotProgramId, binding.ProgramManifestChecksum, binding.GetRequiredCapabilityCodes(),
        binding.CapabilityEvidenceStatus.ToString(),
        binding.Assurance.ToString(),
        binding.GetSupportedOptionCodes(), binding.BindingChecksum, binding.Status.ToString(), binding.CreatedAt,
        binding.RetiredAt);
}

public sealed class ProductionProgramBindingHandlers(IProductionProgramBindingStore store)
{
    public async Task<ApiResult<IReadOnlyList<ProductionProgramBindingResult>>> ListAsync(
        CurrentUserContext userContext, Guid organizationId, ProductionProgramBindingStatus? status,
        CancellationToken cancellationToken)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleaseRead, userContext, organizationId, null, null))
            return ApiResult<IReadOnlyList<ProductionProgramBindingResult>>.Fail("Access denied.", 403);
        var bindings = await store.ListAsync(organizationId, status, cancellationToken);
        return ApiResult<IReadOnlyList<ProductionProgramBindingResult>>.Success(bindings.Select(ProductionProgramBindingResult.From).ToArray());
    }

    public async Task<ApiResult<ProductionProgramBindingResult>> CreateAsync(
        CreateProductionProgramBindingCommand command, CancellationToken cancellationToken)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleasePublish, command.UserContext, command.OrganizationId, null, null))
            return ApiResult<ProductionProgramBindingResult>.Fail("Access denied.", 403);

        var recipe = await store.GetRecipeAsync(command.RecipeId, cancellationToken);
        if (recipe is null || recipe.Status is not (RecipeStatus.Published or RecipeStatus.Active) ||
            (recipe.OrganizationId.HasValue && recipe.OrganizationId != command.OrganizationId))
            return ApiResult<ProductionProgramBindingResult>.Fail("Binding requires a published or active recipe in organization scope.", 400);
        if (recipe.ProductVariant is null || recipe.ProductVariant.Product is null ||
            recipe.ProductVariant.FulfillmentType != FulfillmentType.MachineProduced ||
            (recipe.ProductVariant.Product.OrganizationId.HasValue && recipe.ProductVariant.Product.OrganizationId != command.OrganizationId))
            return ApiResult<ProductionProgramBindingResult>.Fail("Binding requires a machine-produced product variant in organization scope.", 400);

        var program = await store.GetRobotProgramAsync(command.RobotProgramId, cancellationToken);
        if (program is null || program.Status != RobotProgramStatus.Published ||
            string.IsNullOrWhiteSpace(program.ProgramManifestChecksum) ||
            (program.OrganizationId.HasValue && program.OrganizationId != command.OrganizationId))
            return ApiResult<ProductionProgramBindingResult>.Fail("Binding requires a published robot program in organization scope.", 400);

        var validOptionCodes = recipe.ProductVariant.Product.OptionGroups.SelectMany(group => group.ProductOptions)
            .Where(option => option.DeletedAt is null && option.ExecutionImpact == ProductOptionExecutionImpact.ProductionAffecting)
            .Select(option => option.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requestedOptions = command.SupportedOptionCodes.Select(code => code?.Trim()).Where(code => !string.IsNullOrWhiteSpace(code))
            .Cast<string>().ToArray();
        if (requestedOptions.Length != command.SupportedOptionCodes.Count ||
            requestedOptions.Distinct(StringComparer.OrdinalIgnoreCase).Count() != requestedOptions.Length ||
            requestedOptions.Any(code => !validOptionCodes.Contains(code)))
            return ApiResult<ProductionProgramBindingResult>.Fail("Supported option codes must be unique production-affecting options of the recipe product.", 400);

        ProductionProgramCapabilityProposal capabilityProposal;
        try
        {
            capabilityProposal = await store.GetProgramCapabilityProposalAsync(program, cancellationToken);
        }
        catch (DomainRuleException exception)
        {
            return ApiResult<ProductionProgramBindingResult>.Fail(exception.Message, 400);
        }
        var existing = await store.FindActiveEquivalentAsync(command.OrganizationId, recipe.Id, program.Id,
            capabilityProposal.DeclaredRequiredCapabilityCodes, capabilityProposal.Status,
            ProductionProgramBindingAssurance.OperatorDeclared, requestedOptions, cancellationToken);
        if (existing is not null)
            return ApiResult<ProductionProgramBindingResult>.Success(ProductionProgramBindingResult.From(existing),
                "Equivalent production binding already exists.");

        var binding = ProductionProgramBinding.Create(command.OrganizationId, recipe.ProductVariantId, recipe.Id,
            recipe.Version, program.Id, program.ProgramManifestChecksum, capabilityProposal.DeclaredRequiredCapabilityCodes,
            capabilityProposal.Status, ProductionProgramBindingAssurance.OperatorDeclared,
            requestedOptions, command.UserContext.AccountId);
        await store.AddAsync(binding, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return ApiResult<ProductionProgramBindingResult>.Success(ProductionProgramBindingResult.From(binding),
            "Production program binding created.", 201);
    }

    public async Task<ApiResult<ProductionProgramBindingResult>> RetireAsync(
        RetireProductionProgramBindingCommand command, CancellationToken cancellationToken)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ReleasePublish, command.UserContext, command.OrganizationId, null, null))
            return ApiResult<ProductionProgramBindingResult>.Fail("Access denied.", 403);
        var binding = await store.GetAsync(command.OrganizationId, command.BindingId, cancellationToken);
        if (binding is null) return ApiResult<ProductionProgramBindingResult>.Fail("Production program binding not found.", 404);
        binding.Retire(DateTimeOffset.UtcNow, command.UserContext.AccountId);
        await store.SaveChangesAsync(cancellationToken);
        return ApiResult<ProductionProgramBindingResult>.Success(ProductionProgramBindingResult.From(binding),
            "Production program binding retired.");
    }
}
