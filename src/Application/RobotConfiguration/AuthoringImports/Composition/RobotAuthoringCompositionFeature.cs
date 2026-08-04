using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Identity.Tokens.Claims;
using Application.RobotConfiguration.Programs.Commands;
using Application.RobotConfiguration.Programs.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Common;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.AuthoringImports;
using Domain.RobotConfiguration.Programs;

namespace Application.RobotConfiguration.AuthoringImports.Composition;

public interface IRobotAuthoringCompositionStore
{
    Task<Recipe?> GetRecipeAsync(Guid organizationId, Guid recipeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Recipe>> ListEligibleRecipesAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<RobotProgram?> GetProgramAsync(Guid organizationId, Guid programId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RobotArtifact>> GetArtifactsAsync(Guid organizationId, IReadOnlyCollection<Guid> artifactIds,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<RobotArtifactTechnicalContract>> GetContractsAsync(Guid organizationId,
        IReadOnlyCollection<Guid> contractIds, CancellationToken cancellationToken);
}

public sealed record PreviewRobotAuthoringCompositionQuery(CurrentUserContext UserContext, Guid OrganizationId,
    Guid ImportId, Guid RecipeId, IReadOnlyCollection<string> SelectedOptionCodes);

public sealed record ConfirmRobotAuthoringCompositionCommand(CurrentUserContext UserContext, Guid OrganizationId,
    Guid ImportId, Guid RecipeId, IReadOnlyCollection<string> SelectedOptionCodes, string PreviewChecksum);

public sealed record RobotCompositionIssue(string Code, string Message, string? ReferenceCode = null);
public sealed record RobotCompositionRequirement(string Kind, string Code, string? IngredientCode,
    string? OptionCode, decimal? Quantity, string? Unit, string? RequiredWorkcellCapabilityCode,
    string Status, IReadOnlyCollection<string> CandidateArtifactCodes);
public sealed record RobotCompositionArtifactProposal(Guid RobotArtifactId, string ArtifactCode, int RunOrder,
    string? RequiredOptionCode, IReadOnlyCollection<string> EffectCodes);
public sealed record RobotAuthoringCompositionPreview(Guid ImportId, Guid RobotProgramId, Guid RecipeId,
    DateTimeOffset ProgramLastModifiedAt, IReadOnlyCollection<string> SelectedOptionCodes, bool CanConfirm, string PreviewChecksum,
    IReadOnlyCollection<RobotCompositionRequirement> Requirements,
    IReadOnlyCollection<RobotCompositionArtifactProposal> ProposedArtifacts,
    IReadOnlyCollection<string> SuggestedCapabilityCodes,
    IReadOnlyCollection<RobotCompositionIssue> Blockers,
    IReadOnlyCollection<RobotCompositionIssue> Warnings);

public sealed class RobotAuthoringCompositionHandlers(
    IRobotAuthoringImportStore importStore,
    IRobotAuthoringCompositionStore compositionStore,
    ReplaceRobotProgramArtifactsCommandHandler replaceProgramArtifactsHandler)
{
    public async Task<ApiResult<RobotAuthoringCompositionPreview>> PreviewAsync(
        PreviewRobotAuthoringCompositionQuery query, CancellationToken cancellationToken)
    {
        if (!CanAuthor(query.UserContext, query.OrganizationId))
            return ApiResult<RobotAuthoringCompositionPreview>.Fail(
                "Both artifact.upload and program.manage access are required.", 403);

        var result = await BuildPreviewAsync(query.OrganizationId, query.ImportId, query.RecipeId,
            query.SelectedOptionCodes, cancellationToken);
        return result.Error is not null
            ? ApiResult<RobotAuthoringCompositionPreview>.Fail(result.Error.Value.Message, result.Error.Value.StatusCode)
            : ApiResult<RobotAuthoringCompositionPreview>.Success(result.Preview!, "Robot program composition preview generated.");
    }

    public async Task<ApiResult<RobotProgramResult>> ConfirmAsync(
        ConfirmRobotAuthoringCompositionCommand command, CancellationToken cancellationToken)
    {
        if (!CanAuthor(command.UserContext, command.OrganizationId))
            return ApiResult<RobotProgramResult>.Fail("Both artifact.upload and program.manage access are required.", 403);
        if (string.IsNullOrWhiteSpace(command.PreviewChecksum))
            return ApiResult<RobotProgramResult>.Fail("Preview checksum is required.", 400);

        var transactionStarted = false;
        try
        {
            var trackedImport = await importStore.BeginMutationAsync(command.OrganizationId,
                command.ImportId, cancellationToken);
            transactionStarted = true;
            if (trackedImport is null)
                return await RollbackConfirmFailureAsync("Robot authoring import not found.", 404, cancellationToken);

            var result = await BuildPreviewAsync(command.OrganizationId, command.ImportId, command.RecipeId,
                command.SelectedOptionCodes, cancellationToken);
            if (result.Error is not null)
                return await RollbackConfirmFailureAsync(result.Error.Value.Message, result.Error.Value.StatusCode,
                    cancellationToken);
            var preview = result.Preview!;
            if (!string.Equals(preview.PreviewChecksum, command.PreviewChecksum.Trim(), StringComparison.Ordinal))
                return await RollbackConfirmFailureAsync(
                    "Composition inputs changed; generate a new preview before confirmation.", 409, cancellationToken);
            if (!preview.CanConfirm)
                return await RollbackConfirmFailureAsync(
                    "Composition has unresolved blockers and cannot be confirmed.", 409, cancellationToken);

            var replacement = await replaceProgramArtifactsHandler.HandleAsync(new ReplaceRobotProgramArtifactsCommand
            {
                UserContext = command.UserContext,
                OrganizationId = command.OrganizationId,
                ProgramId = preview.RobotProgramId,
                ExpectedLastModifiedAt = preview.ProgramLastModifiedAt,
                Artifacts = preview.ProposedArtifacts.Select(item => new RobotProgramArtifactInput(
                    item.RobotArtifactId, item.RunOrder, 1, null, item.RequiredOptionCode)).ToArray()
            }, cancellationToken);
            if (!replacement.Succeeded)
                return await RollbackConfirmFailureAsync(replacement.Message ?? "Program composition could not be confirmed.",
                    replacement.StatusCode, cancellationToken);

            trackedImport.ConfirmComposition(command.RecipeId, command.SelectedOptionCodes,
                preview.PreviewChecksum, DateTimeOffset.UtcNow, command.UserContext.AccountId);
            await importStore.CommitMutationAsync(cancellationToken);
            transactionStarted = false;
            return replacement;
        }
        catch (DomainRuleException ex)
        {
            if (transactionStarted) await importStore.RollbackMutationAsync(CancellationToken.None);
            return ApiResult<RobotProgramResult>.Fail(ex.Message, 409);
        }
        catch
        {
            if (transactionStarted) await importStore.RollbackMutationAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<ApiResult<RobotProgramResult>> RollbackConfirmFailureAsync(string message, int statusCode,
        CancellationToken cancellationToken)
    {
        await importStore.RollbackMutationAsync(CancellationToken.None);
        return ApiResult<RobotProgramResult>.Fail(message, statusCode);
    }

    private async Task<(RobotAuthoringCompositionPreview? Preview, (string Message, int StatusCode)? Error)> BuildPreviewAsync(
        Guid organizationId, Guid importId, Guid recipeId, IReadOnlyCollection<string> selectedOptionCodes,
        CancellationToken cancellationToken)
    {
        var importSession = await importStore.GetAsync(organizationId, importId, false, cancellationToken);
        if (importSession is null) return (null, ("Robot authoring import not found.", 404));
        if (importSession.Status != RobotAuthoringImportStatus.Applied || !importSession.AppliedRobotProgramId.HasValue)
            return (null, ("Import must be materialized before composition preview.", 409));

        var recipe = await compositionStore.GetRecipeAsync(organizationId, recipeId, cancellationToken);
        if (recipe is null) return (null, ("Published or active recipe was not found in the organization scope.", 404));
        var program = await compositionStore.GetProgramAsync(organizationId, importSession.AppliedRobotProgramId.Value,
            cancellationToken);
        if (program is null) return (null, ("Imported robot program was not found.", 409));

        var blockers = new List<RobotCompositionIssue>();
        var warnings = new List<RobotCompositionIssue>();
        if (program.Status != RobotProgramStatus.Draft)
            blockers.Add(new("PROGRAM_NOT_DRAFT", "Only a Draft robot program can accept a composition proposal.", program.Code));

        var importedArtifactIds = importSession.Items.Where(item => item.RobotArtifactId.HasValue)
            .Select(item => item.RobotArtifactId!.Value).ToHashSet();
        var programRunOrders = program.RobotProgramArtifacts
            .GroupBy(item => item.RobotArtifactId)
            .ToDictionary(group => group.Key, group => group.Single().RunOrder);
        if (programRunOrders.Count != importedArtifactIds.Count ||
            !importedArtifactIds.All(programRunOrders.ContainsKey))
        {
            blockers.Add(new("PROGRAM_ARTIFACT_SET_MISMATCH",
                "The Draft robot program no longer contains exactly the artifacts materialized by this import.", program.Code));
        }

        var normalizedOptions = selectedOptionCodes.Select(Normalize).Where(code => code.Length > 0).ToArray();
        if (normalizedOptions.Length != selectedOptionCodes.Count ||
            normalizedOptions.Distinct(StringComparer.Ordinal).Count() != normalizedOptions.Length)
            return (null, ("Selected option codes must be non-empty and unique.", 400));

        var availableOptions = recipe.ProductVariant.Product.OptionGroups.SelectMany(group => group.ProductOptions)
            .Where(option => option.DeletedAt == null && option.IsAvailable &&
                option.ExecutionImpact == ProductOptionExecutionImpact.ProductionAffecting)
            .ToDictionary(option => Normalize(option.Code), StringComparer.Ordinal);
        var unknownOptions = normalizedOptions.Where(code => !availableOptions.ContainsKey(code)).ToArray();
        if (unknownOptions.Length > 0)
            return (null, ($"Unknown or unavailable production option codes: {string.Join(", ", unknownOptions)}.", 400));

        var artifactIds = importSession.Items.Where(item => item.RobotArtifactId.HasValue)
            .Select(item => item.RobotArtifactId!.Value).Distinct().ToArray();
        var contractIds = importSession.Items.Where(item => item.TechnicalContractId.HasValue)
            .Select(item => item.TechnicalContractId!.Value).Distinct().ToArray();
        if (artifactIds.Length != importSession.Items.Count || contractIds.Length != importSession.Items.Count)
            blockers.Add(new("IMPORT_RESOURCE_INCOMPLETE", "Every import item requires resolved artifact and technical contract identities."));

        var artifacts = await compositionStore.GetArtifactsAsync(organizationId, artifactIds, cancellationToken);
        var contracts = await compositionStore.GetContractsAsync(organizationId, contractIds, cancellationToken);
        var artifactsById = artifacts.ToDictionary(artifact => artifact.Id);
        var contractsById = contracts.ToDictionary(contract => contract.Id);
        var candidates = importSession.Items.OrderBy(item => item.RunOrder).Select(item =>
        {
            artifactsById.TryGetValue(item.RobotArtifactId ?? Guid.Empty, out var artifact);
            contractsById.TryGetValue(item.TechnicalContractId ?? Guid.Empty, out var contract);
            return new Candidate(item, artifact, contract,
                item.RobotArtifactId.HasValue && programRunOrders.TryGetValue(item.RobotArtifactId.Value, out var runOrder)
                    ? runOrder
                    : item.RunOrder);
        }).ToArray();
        foreach (var candidate in candidates.Where(candidate => candidate.Artifact is null || candidate.Contract is null))
            blockers.Add(new("IMPORT_RESOURCE_MISSING", "Resolved import resource no longer exists.", candidate.Item.ArtifactCode));

        var requirements = BuildRequirements(recipe, normalizedOptions, availableOptions);
        var requirementResults = new List<RobotCompositionRequirement>();
        foreach (var requirement in requirements)
        {
            var matches = candidates.Where(candidate => candidate.Contract is { SchemaVersion: >= 2 } &&
                candidate.Contract.Effects.Any(effect => Matches(requirement, effect))).ToArray();
            var status = matches.Length switch { 0 => "Missing", 1 => "Resolved", _ => "Ambiguous" };
            requirementResults.Add(new(requirement.Kind, requirement.Code, requirement.IngredientCode,
                requirement.OptionCode, requirement.Quantity, requirement.Unit,
                requirement.RequiredWorkcellCapabilityCode, status,
                matches.Select(match => match.Item.ArtifactCode).ToArray()));
            if (matches.Length == 0)
                blockers.Add(new("REQUIRED_EFFECT_MISSING", $"No imported artifact satisfies {requirement.Code}.", requirement.Code));
            else if (matches.Length > 1)
                blockers.Add(new("ARTIFACT_CANDIDATE_AMBIGUOUS", $"Multiple imported artifacts satisfy {requirement.Code}.", requirement.Code));
            else
                ValidateQuantity(requirement, matches[0].Contract!.Effects.Where(effect => Matches(requirement, effect)), blockers);
        }

        var selectedOptionSet = normalizedOptions.ToHashSet(StringComparer.Ordinal);
        var included = new List<Candidate>();
        foreach (var candidate in candidates.Where(candidate => candidate.Artifact is not null && candidate.Contract is not null))
        {
            var optionCodes = candidate.Contract!.SchemaVersion >= 2
                ? candidate.Contract.Effects.Select(effect => NormalizeOptional(effect.OptionCode))
                    .Where(code => code is not null).Cast<string>().Distinct(StringComparer.Ordinal).ToArray()
                : [];
            if (optionCodes.Length > 0 && optionCodes.All(code => !selectedOptionSet.Contains(code))) continue;
            if (optionCodes.Any(code => !selectedOptionSet.Contains(code)))
                blockers.Add(new("ARTIFACT_INCLUDES_UNSELECTED_OPTION", "Artifact mixes selected and unselected option effects.", candidate.Item.ArtifactCode));
            if (candidate.Contract.SchemaVersion == 1 || candidate.Contract.Effects.All(effect =>
                    effect.EffectKind is RobotArtifactEffectKind.System or RobotArtifactEffectKind.Motion))
                warnings.Add(new("OPAQUE_ARTIFACT_INCLUDED", "Artifact has no ingredient or option semantics and remains in the proposal.", candidate.Item.ArtifactCode));
            included.Add(candidate);
        }

        var ordered = OrderCandidates(included, blockers);
        var proposal = ordered.Select((candidate, index) => new RobotCompositionArtifactProposal(
            candidate.Artifact!.Id,
            candidate.Artifact.ArtifactCode,
            index + 1,
            candidate.Contract!.SchemaVersion >= 2
                ? ResolveRequiredOptionCode(candidate.Contract, selectedOptionSet)
                : null,
            candidate.Contract!.Effects.Select(effect => effect.EffectCode).Order(StringComparer.Ordinal).ToArray())).ToArray();
        var capabilityCodes = included.Where(candidate => candidate.Contract!.SchemaVersion >= 2)
            .SelectMany(candidate => candidate.Contract!.Effects)
            .Select(effect => NormalizeOptional(effect.RequiredWorkcellCapabilityCode))
            .Where(code => code is not null).Cast<string>().Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        var checksumDocument = new
        {
            importId, recipeId, Options = normalizedOptions.Order(StringComparer.Ordinal),
            Requirements = requirements.OrderBy(requirement => requirement.Code, StringComparer.Ordinal)
                .Select(requirement => new { requirement.Kind, requirement.Code, requirement.IngredientCode,
                    requirement.OptionCode, requirement.Quantity, requirement.Unit,
                    requirement.RequiredWorkcellCapabilityCode }),
            Artifacts = proposal.Select(item => new { item.RobotArtifactId, item.RunOrder, item.RequiredOptionCode }),
            Sidecars = importSession.Items.OrderBy(item => item.RunOrder).Select(item => item.SidecarChecksum)
        };
        var checksumJson = JsonSerializer.Serialize(checksumDocument);
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(checksumJson))).ToLowerInvariant();
        return (new RobotAuthoringCompositionPreview(importId, program.Id, recipeId,
            program.UpdatedAt ?? program.CreatedAt, normalizedOptions,
            blockers.Count == 0, checksum, requirementResults, proposal, capabilityCodes, blockers, warnings), null);
    }

    private static IReadOnlyList<RequiredEffect> BuildRequirements(Recipe recipe, IReadOnlyCollection<string> options,
        IReadOnlyDictionary<string, ProductOption> availableOptions)
    {
        var requirements = recipe.RecipeItems.Where(item => item.DeletedAt == null && !item.IsOptional)
            .Select(item => new RequiredEffect("Ingredient", $"INGREDIENT:{Normalize(item.Ingredient.Code)}",
                Normalize(item.Ingredient.Code), null, item.Quantity, NormalizeUnit(item.Unit), null)).ToList();
        foreach (var optionCode in options)
        {
            var option = availableOptions[optionCode];
            var optionRequirements = option.IngredientRequirements.Where(item => item.DeletedAt == null).ToArray();
            if (optionRequirements.Length == 0)
                requirements.Add(new("Option", $"OPTION:{optionCode}", null, optionCode, null, null, null));
            else
                requirements.AddRange(optionRequirements.Select(item => new RequiredEffect("OptionIngredient",
                    $"OPTION:{optionCode}:INGREDIENT:{Normalize(item.Ingredient.Code)}", Normalize(item.Ingredient.Code),
                    optionCode, item.Quantity, NormalizeUnit(item.Unit), Normalize(item.RequiredWorkcellCapabilityCode))));
        }
        return requirements;
    }

    private static bool Matches(RequiredEffect requirement, RobotArtifactDeclaredEffect effect) =>
        (requirement.Kind == "Ingredient" ? effect.EffectKind == RobotArtifactEffectKind.Ingredient :
            requirement.Kind == "Option" ? effect.EffectKind == RobotArtifactEffectKind.Option :
            requirement.Kind == "OptionIngredient" ? effect.EffectKind is RobotArtifactEffectKind.Ingredient or RobotArtifactEffectKind.Option : true) &&
        (requirement.IngredientCode is null || NormalizeOptional(effect.IngredientCode) == requirement.IngredientCode) &&
        (requirement.OptionCode is null
            ? NormalizeOptional(effect.OptionCode) is null
            : NormalizeOptional(effect.OptionCode) == requirement.OptionCode) &&
        (requirement.RequiredWorkcellCapabilityCode is null ||
         NormalizeOptional(effect.RequiredWorkcellCapabilityCode) == requirement.RequiredWorkcellCapabilityCode);

    private static void ValidateQuantity(RequiredEffect requirement, IEnumerable<RobotArtifactDeclaredEffect> effects,
        ICollection<RobotCompositionIssue> blockers)
    {
        if (!requirement.Quantity.HasValue) return;
        var matching = effects.ToArray();
        if (matching.Any(effect => effect.QuantityMode == RobotArtifactQuantityMode.Parameterized))
            blockers.Add(new("PARAMETERIZED_QUANTITY_UNSUPPORTED", "Current Fairino runtime does not prove parameterized quantity support.", requirement.Code));
        else if (matching.All(effect => effect.QuantityMode == RobotArtifactQuantityMode.None))
            blockers.Add(new("QUANTITY_AUTHORITY_UNPROVEN", "Ingredient quantity is not declared by the artifact contract.", requirement.Code));
        else if (!matching.Any(effect => effect.QuantityMode == RobotArtifactQuantityMode.FixedInArtifact &&
                     effect.FixedQuantity == requirement.Quantity && NormalizeUnit(effect.Unit) == requirement.Unit))
            blockers.Add(new("FIXED_QUANTITY_MISMATCH", "Fixed artifact quantity or unit does not match the Recipe/Option requirement.", requirement.Code));
    }

    private static IReadOnlyList<Candidate> OrderCandidates(IReadOnlyCollection<Candidate> candidates,
        ICollection<RobotCompositionIssue> blockers)
    {
        var effectOwners = candidates.SelectMany(candidate => candidate.Contract!.Effects.Select(effect => (effect.EffectCode, Candidate: candidate)))
            .GroupBy(item => Normalize(item.EffectCode)).ToDictionary(group => group.Key, group => group.Select(item => item.Candidate).Distinct().ToArray());
        var edges = candidates.ToDictionary(candidate => candidate.Item.Id, _ => new HashSet<Guid>());
        var indegree = candidates.ToDictionary(candidate => candidate.Item.Id, _ => 0);
        foreach (var candidate in candidates)
        foreach (var constraint in candidate.Contract!.OrderingConstraints.Where(constraint => constraint.ConstraintType != RobotArtifactOrderingConstraintType.Phase))
        {
            if (!effectOwners.TryGetValue(Normalize(constraint.Value), out var owners) || owners.Length != 1)
            {
                blockers.Add(new("ORDERING_EFFECT_UNRESOLVED", "Ordering target is missing or ambiguous.", constraint.Value));
                continue;
            }
            var from = constraint.ConstraintType == RobotArtifactOrderingConstraintType.BeforeEffect ? candidate : owners[0];
            var to = constraint.ConstraintType == RobotArtifactOrderingConstraintType.BeforeEffect ? owners[0] : candidate;
            if (from.Item.Id != to.Item.Id && edges[from.Item.Id].Add(to.Item.Id)) indegree[to.Item.Id]++;
        }

        var ordered = new List<Candidate>();
        while (ordered.Count < candidates.Count)
        {
            var next = candidates.Where(candidate => !ordered.Contains(candidate) && indegree[candidate.Item.Id] == 0)
                .OrderBy(PhaseRank).ThenBy(SortHint).ThenBy(candidate => candidate.ProgramRunOrder)
                .ThenBy(candidate => candidate.Item.ArtifactCode, StringComparer.Ordinal).FirstOrDefault();
            if (next is null)
            {
                blockers.Add(new("ORDERING_CYCLE", "Artifact ordering constraints contain a cycle."));
                return candidates.OrderBy(candidate => candidate.ProgramRunOrder).ToArray();
            }
            ordered.Add(next);
            foreach (var target in edges[next.Item.Id]) indegree[target]--;
        }
        return ordered;
    }

    private static int PhaseRank(Candidate candidate) => candidate.Contract!.OrderingConstraints
        .Where(constraint => constraint.ConstraintType == RobotArtifactOrderingConstraintType.Phase)
        .Select(constraint => Normalize(constraint.Value) switch
        { "PREPARE" => 0, "BASE" => 1, "OPTION" => 2, "DELIVER" => 3, "CLEANUP" => 4, _ => 5 })
        .DefaultIfEmpty(1).Min();
    private static int SortHint(Candidate candidate) => candidate.Contract!.OrderingConstraints
        .Where(constraint => constraint.ConstraintType == RobotArtifactOrderingConstraintType.Phase)
        .Select(constraint => constraint.SortHint).DefaultIfEmpty(candidate.ProgramRunOrder).Min();
    private static string? ResolveRequiredOptionCode(RobotArtifactTechnicalContract contract, IReadOnlySet<string> selectedOptions)
    {
        var values = contract.Effects.Select(effect => NormalizeOptional(effect.OptionCode)).Where(code => code is not null)
            .Cast<string>().Distinct(StringComparer.Ordinal).Where(selectedOptions.Contains).ToArray();
        return values.Length == 1 ? values[0] : null;
    }

    private static bool CanAuthor(CurrentUserContext user, Guid organizationId) =>
        ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactUpload, user, organizationId, null, null) &&
        ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProgramManage, user, organizationId, null, null);
    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : Normalize(value);
    private static string? NormalizeUnit(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private sealed record RequiredEffect(string Kind, string Code, string? IngredientCode, string? OptionCode,
        decimal? Quantity, string? Unit, string? RequiredWorkcellCapabilityCode);
    private sealed record Candidate(RobotAuthoringImportItem Item, RobotArtifact? Artifact,
        RobotArtifactTechnicalContract? Contract, int ProgramRunOrder);
}
