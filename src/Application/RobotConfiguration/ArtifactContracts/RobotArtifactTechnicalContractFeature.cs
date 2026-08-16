using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Shared.Ownership;
using Application.Tenants;
using Domain.Common;
using Domain.RobotConfiguration.ArtifactContracts;
using Application.RobotConfiguration.ArtifactTemplates.Abstractions;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.Shared.Concurrency;

namespace Application.RobotConfiguration.ArtifactContracts;

public interface IRobotArtifactTechnicalContractStore
{
    Task<RobotArtifactTechnicalContract?> GetAsync(Guid id, bool tracked, CancellationToken cancellationToken);
    Task<RobotArtifactTechnicalContract?> GetByIdentityAsync(
        Guid? organizationId, string code, int version, bool tracked, CancellationToken cancellationToken);
    Task<int> CountAsync(Guid? organizationId, RobotArtifactContractStatus? status, string? search,
        bool publishedOnly, CancellationToken cancellationToken);
    Task<IReadOnlyList<RobotArtifactTechnicalContract>> ListAsync(
        Guid? organizationId, RobotArtifactContractStatus? status, string? search, bool publishedOnly,
        int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<bool> VersionExistsAsync(Guid? organizationId, string code, int version, CancellationToken cancellationToken);
    Task<bool> HasPublishedTemplateReferenceAsync(Guid contractId, CancellationToken cancellationToken);
    Task<bool> HasAuthoringImportReferenceAsync(Guid contractId, CancellationToken cancellationToken);
    Task AddAsync(RobotArtifactTechnicalContract contract, CancellationToken cancellationToken);
    void Remove(RobotArtifactTechnicalContract contract);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record RobotArtifactEffectRequest(
    string EffectCode,
    RobotArtifactEffectKind EffectKind,
    string? IngredientCode,
    string? OptionCode,
    RobotArtifactQuantityMode QuantityMode,
    decimal? FixedQuantity,
    string? Unit,
    string? RequiredWorkcellCapabilityCode);

public sealed record RobotArtifactOrderingConstraintRequest(
    RobotArtifactOrderingConstraintType ConstraintType,
    string Value,
    int SortHint);

public sealed record RobotArtifactTechnicalContractResult(
    Guid Id,
    Guid? OrganizationId,
    string ContractCode,
    int ContractVersion,
    int SchemaVersion,
    string RuntimeTargetCode,
    string MachineModelCode,
    string Status,
    string? ContractChecksum,
    IReadOnlyCollection<RobotArtifactEffectRequest> Effects,
    IReadOnlyCollection<RobotArtifactOrderingConstraintRequest> OrderingConstraints)
{
    public static RobotArtifactTechnicalContractResult From(RobotArtifactTechnicalContract contract) => new(
        contract.Id, contract.OrganizationId, contract.ContractCode, contract.ContractVersion,
        contract.SchemaVersion, contract.RuntimeTargetCode, contract.MachineModelCode,
        contract.Status.ToString(), contract.ContractChecksum,
        contract.Effects.Select(x => new RobotArtifactEffectRequest(x.EffectCode, x.EffectKind, x.IngredientCode,
            x.OptionCode, x.QuantityMode, x.FixedQuantity, x.Unit, x.RequiredWorkcellCapabilityCode)).ToArray(),
        contract.OrderingConstraints.Select(x => new RobotArtifactOrderingConstraintRequest(
            x.ConstraintType, x.Value, x.SortHint)).ToArray());
}

public sealed class CreateRobotArtifactTechnicalContractCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid? OrganizationId { get; init; }
    public required string ContractCode { get; init; }
    public int ContractVersion { get; init; }
    public int SchemaVersion { get; init; } = 1;
    public required string RuntimeTargetCode { get; init; }
    public required string MachineModelCode { get; init; }
    public IReadOnlyCollection<RobotArtifactEffectRequest> Effects { get; init; } = [];
    public IReadOnlyCollection<RobotArtifactOrderingConstraintRequest> OrderingConstraints { get; init; } = [];
}

public sealed class ImportRobotArtifactTechnicalContractSidecarCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string ContractCode { get; init; }
    public int ContractVersion { get; init; }
    public int SchemaVersion { get; init; } = 1;
    public required string RuntimeTargetCode { get; init; }
    public required string MachineModelCode { get; init; }
    public IReadOnlyCollection<RobotArtifactEffectRequest> Effects { get; init; } = [];
    public IReadOnlyCollection<RobotArtifactOrderingConstraintRequest> OrderingConstraints { get; init; } = [];
}

public sealed record PublishRobotArtifactTechnicalContractCommand(
    CurrentUserContext UserContext, Guid? OrganizationId, Guid ContractId);

public sealed record GetRobotArtifactTechnicalContractQuery(
    CurrentUserContext UserContext, Guid? OrganizationId, Guid ContractId);

public sealed record ReplaceRobotArtifactTechnicalContractCommand(
    CurrentUserContext UserContext,
    Guid? OrganizationId,
    Guid ContractId,
    IReadOnlyCollection<RobotArtifactEffectRequest> Effects,
    IReadOnlyCollection<RobotArtifactOrderingConstraintRequest> OrderingConstraints);

public sealed record ValidateRobotArtifactTechnicalContractCommand(
    CurrentUserContext UserContext, Guid? OrganizationId, Guid ContractId);

public sealed record RetireRobotArtifactTechnicalContractCommand(
    CurrentUserContext UserContext, Guid? OrganizationId, Guid ContractId);

public sealed record DiscardRobotArtifactTechnicalContractCommand(
    CurrentUserContext UserContext, Guid? OrganizationId, Guid ContractId);

public sealed record ListRobotArtifactTechnicalContractsQuery(
    CurrentUserContext UserContext,
    Guid? OrganizationId,
    RobotArtifactContractStatus? Status = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed class RobotArtifactTechnicalContractHandlers(
    IRobotArtifactTechnicalContractStore store,
    ITechnicalResourceMutationCoordinator mutationCoordinator)
{
    private ITechnicalResourceMutationCoordinator Mutations { get; } = mutationCoordinator;
    public async Task<ApiResult<RobotArtifactTechnicalContractResult>> GetAsync(
        GetRobotArtifactTechnicalContractQuery query, CancellationToken cancellationToken)
    {
        if (!CanRead(query.UserContext, query.OrganizationId))
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Access denied.", 403);
        var contract = await store.GetAsync(query.ContractId, false, cancellationToken);
        var publishedOnly = query.OrganizationId is null && !query.UserContext.IsSystemAdmin;
        return contract is null || contract.OrganizationId != query.OrganizationId ||
            (publishedOnly && contract.Status != RobotArtifactContractStatus.Published)
            ? ApiResult<RobotArtifactTechnicalContractResult>.Fail("Technical contract not found.", 404)
            : ApiResult<RobotArtifactTechnicalContractResult>.Success(RobotArtifactTechnicalContractResult.From(contract));
    }

    public async Task<ApiResult<RobotArtifactTechnicalContractResult>> CreateAsync(
        CreateRobotArtifactTechnicalContractCommand command, CancellationToken cancellationToken)
    {
        if (!CanManage(command.UserContext, command.OrganizationId))
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Access denied.", 403);
        var code = command.ContractCode.Trim().ToUpperInvariant();
        return await Mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.ContractDefinition(
                command.OrganizationId, code, command.ContractVersion)],
            ct => CreateUnderIdentityLockAsync(command, code, ct),
            cancellationToken);
    }

    public async Task<ApiResult<RobotArtifactTechnicalContractResult>> ImportSidecarAsync(
        ImportRobotArtifactTechnicalContractSidecarCommand command,
        CancellationToken cancellationToken)
    {
        if (!CanManage(command.UserContext, command.OrganizationId))
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Access denied.", 403);
        var sidecarError = ValidateImportedSidecar(command);
        if (sidecarError is not null)
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail(sidecarError, 400);

        var code = command.ContractCode.Trim().ToUpperInvariant();
        return await Mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.ContractDefinition(
                command.OrganizationId, code, command.ContractVersion)],
            async ct =>
            {
                var observed = await store.GetByIdentityAsync(
                    command.OrganizationId, code, command.ContractVersion, false, ct);
                if (observed is null)
                    return await CreateUnderIdentityLockAsync(new CreateRobotArtifactTechnicalContractCommand
                    {
                        UserContext = command.UserContext,
                        OrganizationId = command.OrganizationId,
                        ContractCode = code,
                        ContractVersion = command.ContractVersion,
                        SchemaVersion = command.SchemaVersion,
                        RuntimeTargetCode = command.RuntimeTargetCode,
                        MachineModelCode = command.MachineModelCode,
                        Effects = command.Effects,
                        OrderingConstraints = command.OrderingConstraints
                    }, code, ct);

                return await Mutations.ExecuteAsync(
                    [TechnicalResourceMutationIdentity.Contract(observed.Id)],
                    async lockedCt =>
                    {
                        var existing = await store.GetAsync(observed.Id, true, lockedCt);
                        if (existing is null || existing.OrganizationId != command.OrganizationId ||
                            existing.ContractCode != code || existing.ContractVersion != command.ContractVersion)
                            return ApiResult<RobotArtifactTechnicalContractResult>.Fail(
                                "Technical contract identity changed concurrently; retry sidecar import.", 409);
                        if (existing.Status != RobotArtifactContractStatus.Draft)
                            return ApiResult<RobotArtifactTechnicalContractResult>.Fail(
                                "The technical contract version is already published or retired. Import a new version instead.", 409);
                        if (!string.Equals(existing.RuntimeTargetCode, command.RuntimeTargetCode.Trim(), StringComparison.OrdinalIgnoreCase) ||
                            !string.Equals(existing.MachineModelCode, command.MachineModelCode.Trim(), StringComparison.OrdinalIgnoreCase))
                            return ApiResult<RobotArtifactTechnicalContractResult>.Fail(
                                "A Draft technical contract cannot change runtime target or machine model through sidecar import.", 409);
                        if (existing.SchemaVersion != command.SchemaVersion)
                            return ApiResult<RobotArtifactTechnicalContractResult>.Fail(
                                "A Draft technical contract cannot change schema version through sidecar import.", 409);
                        try
                        {
                            existing.ReplaceDefinition(ToEffects(command.Effects), ToConstraints(command.OrderingConstraints));
                            existing.UpdatedByAccountId = command.UserContext.AccountId;
                            await store.SaveChangesAsync(lockedCt);
                            return ApiResult<RobotArtifactTechnicalContractResult>.Success(
                                RobotArtifactTechnicalContractResult.From(existing),
                                "Draft technical contract replaced from sidecar.");
                        }
                        catch (DomainRuleException exception)
                        {
                            return ApiResult<RobotArtifactTechnicalContractResult>.Fail(exception.Message, 400);
                        }
                    }, ct);
            }, cancellationToken);
    }

    private async Task<ApiResult<RobotArtifactTechnicalContractResult>> CreateUnderIdentityLockAsync(
        CreateRobotArtifactTechnicalContractCommand command,
        string normalizedCode,
        CancellationToken cancellationToken)
    {
        if (await store.VersionExistsAsync(
                command.OrganizationId, normalizedCode, command.ContractVersion, cancellationToken))
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail(
                "Technical contract version already exists.", 409);
        try
        {
            var contract = RobotArtifactTechnicalContract.CreateDraft(
                normalizedCode, command.ContractVersion, command.RuntimeTargetCode,
                command.MachineModelCode, command.OrganizationId, schemaVersion: command.SchemaVersion);
            contract.CreatedByAccountId = command.UserContext.AccountId;
            contract.ReplaceDefinition(ToEffects(command.Effects), ToConstraints(command.OrderingConstraints));
            await store.AddAsync(contract, cancellationToken);
            return ApiResult<RobotArtifactTechnicalContractResult>.Success(
                RobotArtifactTechnicalContractResult.From(contract), "Technical contract Draft created.", 201);
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail(ex.Message, 400);
        }
    }

    private static string? ValidateImportedSidecar(ImportRobotArtifactTechnicalContractSidecarCommand command)
    {
        if (command.SchemaVersion is not 1 and not 2)
            return "Unsupported IceBot sidecar schema version.";
        if (command.SchemaVersion == 1)
        {
            if (command.Effects.Any(effect =>
                    effect.EffectKind is not RobotArtifactEffectKind.System and not RobotArtifactEffectKind.Motion ||
                    effect.IngredientCode is not null || effect.OptionCode is not null ||
                    effect.QuantityMode != RobotArtifactQuantityMode.None || effect.FixedQuantity.HasValue ||
                    effect.Unit is not null || effect.RequiredWorkcellCapabilityCode is not null))
                return "Sidecar schema version 1 is opaque and may declare only System/Motion effects without production semantics.";
            if (command.OrderingConstraints.Any(constraint =>
                    constraint.ConstraintType != RobotArtifactOrderingConstraintType.Phase))
                return "Sidecar schema version 1 may declare only Phase ordering constraints.";
            return null;
        }

        if (command.Effects.Any(effect => effect.EffectKind == RobotArtifactEffectKind.Composite))
            return "Composite effects are not supported by authoring schema version 2.";
        return null;
    }

    public async Task<ApiResult<RobotArtifactTechnicalContractResult>> PublishAsync(
        PublishRobotArtifactTechnicalContractCommand command, CancellationToken cancellationToken)
    {
        if (!CanManage(command.UserContext, command.OrganizationId))
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Access denied.", 403);
        return await Mutations.ExecuteAsync([TechnicalResourceMutationIdentity.Contract(command.ContractId)], async ct =>
        {
            var contract = await store.GetAsync(command.ContractId, true, ct);
            if (contract is null || contract.OrganizationId != command.OrganizationId)
                return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Technical contract not found.", 404);
            try
            {
                contract.Publish(DateTimeOffset.UtcNow, command.UserContext.AccountId);
                await store.SaveChangesAsync(ct);
                return ApiResult<RobotArtifactTechnicalContractResult>.Success(RobotArtifactTechnicalContractResult.From(contract));
            }
            catch (DomainRuleException ex) { return ApiResult<RobotArtifactTechnicalContractResult>.Fail(ex.Message, 400); }
        }, cancellationToken);
    }

    public async Task<ApiResult<RobotArtifactTechnicalContractResult>> ReplaceAsync(
        ReplaceRobotArtifactTechnicalContractCommand command, CancellationToken cancellationToken)
    {
        if (!CanManage(command.UserContext, command.OrganizationId))
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Access denied.", 403);
        return await Mutations.ExecuteAsync([TechnicalResourceMutationIdentity.Contract(command.ContractId)], async ct =>
        {
            var contract = await store.GetAsync(command.ContractId, true, ct);
            if (contract is null || contract.OrganizationId != command.OrganizationId)
                return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Technical contract not found.", 404);
            try
            {
                contract.ReplaceDefinition(ToEffects(command.Effects), ToConstraints(command.OrderingConstraints));
                contract.UpdatedByAccountId = command.UserContext.AccountId;
                await store.SaveChangesAsync(ct);
                return ApiResult<RobotArtifactTechnicalContractResult>.Success(RobotArtifactTechnicalContractResult.From(contract));
            }
            catch (DomainRuleException ex) { return ApiResult<RobotArtifactTechnicalContractResult>.Fail(ex.Message, 400); }
        }, cancellationToken);
    }

    public async Task<ApiResult<RobotArtifactTechnicalContractResult>> ValidateAsync(
        ValidateRobotArtifactTechnicalContractCommand command, CancellationToken cancellationToken)
    {
        if (!CanManage(command.UserContext, command.OrganizationId))
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Access denied.", 403);
        var contract = await store.GetAsync(command.ContractId, false, cancellationToken);
        if (contract is null || contract.OrganizationId != command.OrganizationId)
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Technical contract not found.", 404);
        try
        {
            contract.ValidateForPublication();
            return ApiResult<RobotArtifactTechnicalContractResult>.Success(
                RobotArtifactTechnicalContractResult.From(contract), "Technical contract is ready for publication.");
        }
        catch (DomainRuleException ex) { return ApiResult<RobotArtifactTechnicalContractResult>.Fail(ex.Message, 400); }
    }

    public async Task<ApiResult<RobotArtifactTechnicalContractResult>> RetireAsync(
        RetireRobotArtifactTechnicalContractCommand command, CancellationToken cancellationToken)
    {
        if (!CanManage(command.UserContext, command.OrganizationId))
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Access denied.", 403);
        return await Mutations.ExecuteAsync([TechnicalResourceMutationIdentity.Contract(command.ContractId)], async ct =>
        {
            var contract = await store.GetAsync(command.ContractId, true, ct);
            if (contract is null || contract.OrganizationId != command.OrganizationId)
                return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Technical contract not found.", 404);
            if (await store.HasPublishedTemplateReferenceAsync(contract.Id, ct))
                return ApiResult<RobotArtifactTechnicalContractResult>.Fail(
                    "Retire published templates that reference this technical contract before retiring the contract.", 409);
            try
            {
                contract.Retire(DateTimeOffset.UtcNow, command.UserContext.AccountId);
                await store.SaveChangesAsync(ct);
                return ApiResult<RobotArtifactTechnicalContractResult>.Success(RobotArtifactTechnicalContractResult.From(contract));
            }
            catch (DomainRuleException ex) { return ApiResult<RobotArtifactTechnicalContractResult>.Fail(ex.Message, 400); }
        }, cancellationToken);
    }

    public async Task<ApiResult<object>> DiscardAsync(
        DiscardRobotArtifactTechnicalContractCommand command, CancellationToken cancellationToken)
    {
        if (!CanManage(command.UserContext, command.OrganizationId))
            return ApiResult<object>.Fail("Access denied.", 403);
        return await Mutations.ExecuteAsync([TechnicalResourceMutationIdentity.Contract(command.ContractId)], async ct =>
        {
            var contract = await store.GetAsync(command.ContractId, true, ct);
            if (contract is null || contract.OrganizationId != command.OrganizationId)
                return ApiResult<object>.Fail("Technical contract not found.", 404);
            if (contract.Status != RobotArtifactContractStatus.Draft)
                return ApiResult<object>.Fail("Only Draft technical contracts can be discarded.", 409);
            if (await store.HasAuthoringImportReferenceAsync(contract.Id, ct))
                return ApiResult<object>.Fail(
                    "Technical contracts materialized by an authoring import must be retained with that import.", 409);
            store.Remove(contract);
            await store.SaveChangesAsync(ct);
            return ApiResult<object>.Success(new { contract.Id }, "Technical contract Draft discarded.");
        }, cancellationToken);
    }

    public async Task<PagedResult<RobotArtifactTechnicalContractResult>> ListAsync(
        ListRobotArtifactTechnicalContractsQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        if (!CanRead(query.UserContext, query.OrganizationId))
            return PagedResult<RobotArtifactTechnicalContractResult>.Forbidden("Access denied.", pageNumber, pageSize);

        var publishedOnly = query.OrganizationId is null && !query.UserContext.IsSystemAdmin;
        var total = await store.CountAsync(
            query.OrganizationId, query.Status, query.Search, publishedOnly, cancellationToken);
        var contracts = await store.ListAsync(
            query.OrganizationId, query.Status, query.Search, publishedOnly,
            pageNumber, pageSize, cancellationToken);
        return PagedResult<RobotArtifactTechnicalContractResult>.Success(
            contracts.Select(RobotArtifactTechnicalContractResult.From), total, pageNumber, pageSize);
    }

    private static bool CanManage(CurrentUserContext user, Guid? organizationId) => organizationId is null
        ? user.IsSystemAdmin
        : ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactUpload, user, organizationId, null, null);

    private static bool CanRead(CurrentUserContext user, Guid? organizationId) => organizationId is null
        ? user.IsSystemAdmin || user.RoleScopes.Any(scope =>
            string.Equals(scope.RoleCode, "OrgAdmin", StringComparison.OrdinalIgnoreCase))
        : ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactRead, user, organizationId, null, null);

    private static RobotArtifactEffectDefinition[] ToEffects(IEnumerable<RobotArtifactEffectRequest> effects) =>
        effects.Select(x => new RobotArtifactEffectDefinition(x.EffectCode, x.EffectKind, x.IngredientCode,
            x.OptionCode, x.QuantityMode, x.FixedQuantity, x.Unit, x.RequiredWorkcellCapabilityCode)).ToArray();

    private static RobotArtifactOrderingConstraintDefinition[] ToConstraints(
        IEnumerable<RobotArtifactOrderingConstraintRequest> constraints) =>
        constraints.Select(x => new RobotArtifactOrderingConstraintDefinition(
            x.ConstraintType, x.Value, x.SortHint)).ToArray();
}

public sealed class AssignRobotArtifactTechnicalContractHandler(
    IRobotArtifactTechnicalContractStore contracts,
    IRobotArtifactTemplateStore templates,
    IRobotArtifactStore artifacts,
    ITechnicalResourceMutationPolicy technicalOwnership,
    ITechnicalResourceMutationCoordinator mutationCoordinator)
{
    private ITechnicalResourceMutationCoordinator Mutations { get; } = mutationCoordinator;

    public async Task<ApiResult<object>> AssignTemplateAsync(CurrentUserContext user, Guid templateId,
        Guid contractId, CancellationToken cancellationToken)
    {
        if (!user.IsSystemAdmin) return ApiResult<object>.Fail("Access denied.", 403);
        return await Mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Contract(contractId), TechnicalResourceMutationIdentity.Template(templateId)],
            async ct =>
            {
                var contract = await contracts.GetAsync(contractId, false, ct);
                var template = await templates.GetByIdAsync(templateId, true, ct);
                if (contract is null || template is null || contract.OrganizationId.HasValue ||
                    contract.Status != RobotArtifactContractStatus.Published || string.IsNullOrWhiteSpace(contract.ContractChecksum))
                    return ApiResult<object>.Fail("Published global technical contract or template not found.", 404);
                try
                {
                    template.AssignTechnicalContract(contract.Id, contract.ContractChecksum);
                    await templates.SaveChangesAsync(ct);
                    return ApiResult<object>.Success(new { template.Id, TechnicalContractId = contract.Id, contract.ContractChecksum });
                }
                catch (DomainRuleException ex) { return ApiResult<object>.Fail(ex.Message, 400); }
            }, cancellationToken);
    }

    public async Task<ApiResult<object>> AssignArtifactAsync(CurrentUserContext user, Guid organizationId,
        Guid artifactId, Guid contractId, CancellationToken cancellationToken)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactUpload, user, organizationId, null, null))
            return ApiResult<object>.Fail("Access denied.", 403);
        return await Mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Artifact(artifactId), TechnicalResourceMutationIdentity.Contract(contractId)],
            async ct =>
            {
                var contract = await contracts.GetAsync(contractId, false, ct);
                var artifact = await artifacts.GetArtifactForPublishAsync(organizationId, artifactId, ct);
                if (contract is null || artifact is null || contract.Status != RobotArtifactContractStatus.Published ||
                    (contract.OrganizationId.HasValue && contract.OrganizationId != organizationId) ||
                    string.IsNullOrWhiteSpace(contract.ContractChecksum))
                    return ApiResult<object>.Fail("Published technical contract or Draft artifact not found.", 404);
                var ownershipError = await technicalOwnership.ValidateDefinitionMutationAsync(
                    TechnicalResourceKind.RobotArtifact, artifact.Id, ct);
                if (ownershipError is not null)
                    return ApiResult<object>.Fail(ownershipError, 409);
                try
                {
                    artifact.AssignTechnicalContract(contract.Id, contract.ContractChecksum);
                    await artifacts.SaveChangesAsync(ct);
                    return ApiResult<object>.Success(new
                        { artifact.Id, TechnicalContractId = contract.Id, contract.ContractChecksum });
                }
                catch (DomainRuleException ex) { return ApiResult<object>.Fail(ex.Message, 400); }
            }, cancellationToken);
    }
}
