using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.RobotConfiguration.ArtifactContracts;
using Application.RobotConfiguration.ArtifactTemplates.Abstractions;
using Application.RobotConfiguration.Artifacts.Abstractions;

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

public sealed class RobotArtifactTechnicalContractHandlers(IRobotArtifactTechnicalContractStore store)
{
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
        if (await store.VersionExistsAsync(command.OrganizationId, command.ContractCode.Trim().ToUpperInvariant(),
                command.ContractVersion, cancellationToken))
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Technical contract version already exists.", 409);

        try
        {
            var contract = RobotArtifactTechnicalContract.CreateDraft(command.ContractCode, command.ContractVersion,
                command.RuntimeTargetCode, command.MachineModelCode, command.OrganizationId);
            contract.CreatedByAccountId = command.UserContext.AccountId;
            contract.ReplaceDefinition(
                command.Effects.Select(x => new RobotArtifactEffectDefinition(x.EffectCode, x.EffectKind,
                    x.IngredientCode, x.OptionCode, x.QuantityMode, x.FixedQuantity, x.Unit,
                    x.RequiredWorkcellCapabilityCode)).ToArray(),
                command.OrderingConstraints.Select(x => new RobotArtifactOrderingConstraintDefinition(
                    x.ConstraintType, x.Value, x.SortHint)).ToArray());
            await store.AddAsync(contract, cancellationToken);
            return ApiResult<RobotArtifactTechnicalContractResult>.Success(
                RobotArtifactTechnicalContractResult.From(contract), "Technical contract Draft created.", 201);
        }
        catch (DomainRuleException ex) { return ApiResult<RobotArtifactTechnicalContractResult>.Fail(ex.Message, 400); }
    }

    public async Task<ApiResult<RobotArtifactTechnicalContractResult>> ImportSidecarAsync(
        ImportRobotArtifactTechnicalContractSidecarCommand command,
        CancellationToken cancellationToken)
    {
        if (!CanManage(command.UserContext, command.OrganizationId))
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Access denied.", 403);

        var code = command.ContractCode.Trim().ToUpperInvariant();
        var existing = await store.GetByIdentityAsync(
            command.OrganizationId,
            code,
            command.ContractVersion,
            true,
            cancellationToken);

        if (existing is null)
        {
            return await CreateAsync(new CreateRobotArtifactTechnicalContractCommand
            {
                UserContext = command.UserContext,
                OrganizationId = command.OrganizationId,
                ContractCode = code,
                ContractVersion = command.ContractVersion,
                RuntimeTargetCode = command.RuntimeTargetCode,
                MachineModelCode = command.MachineModelCode,
                Effects = command.Effects,
                OrderingConstraints = command.OrderingConstraints
            }, cancellationToken);
        }

        if (existing.Status != RobotArtifactContractStatus.Draft)
        {
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail(
                "The technical contract version is already published or retired. Import a new version instead.",
                409);
        }

        if (!string.Equals(existing.RuntimeTargetCode, command.RuntimeTargetCode.Trim(), StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(existing.MachineModelCode, command.MachineModelCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail(
                "A Draft technical contract cannot change runtime target or machine model through sidecar import.",
                409);
        }

        try
        {
            existing.ReplaceDefinition(ToEffects(command.Effects), ToConstraints(command.OrderingConstraints));
            existing.UpdatedByAccountId = command.UserContext.AccountId;
            await store.SaveChangesAsync(cancellationToken);
            return ApiResult<RobotArtifactTechnicalContractResult>.Success(
                RobotArtifactTechnicalContractResult.From(existing),
                "Draft technical contract replaced from sidecar.");
        }
        catch (DomainRuleException exception)
        {
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail(exception.Message, 400);
        }
    }

    public async Task<ApiResult<RobotArtifactTechnicalContractResult>> PublishAsync(
        PublishRobotArtifactTechnicalContractCommand command, CancellationToken cancellationToken)
    {
        if (!CanManage(command.UserContext, command.OrganizationId))
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Access denied.", 403);
        var contract = await store.GetAsync(command.ContractId, true, cancellationToken);
        if (contract is null || contract.OrganizationId != command.OrganizationId)
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Technical contract not found.", 404);
        try
        {
            contract.Publish(DateTimeOffset.UtcNow, command.UserContext.AccountId, parameterizedRuntimeSupported: false);
            await store.SaveChangesAsync(cancellationToken);
            return ApiResult<RobotArtifactTechnicalContractResult>.Success(RobotArtifactTechnicalContractResult.From(contract));
        }
        catch (DomainRuleException ex) { return ApiResult<RobotArtifactTechnicalContractResult>.Fail(ex.Message, 400); }
    }

    public async Task<ApiResult<RobotArtifactTechnicalContractResult>> ReplaceAsync(
        ReplaceRobotArtifactTechnicalContractCommand command, CancellationToken cancellationToken)
    {
        if (!CanManage(command.UserContext, command.OrganizationId))
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Access denied.", 403);
        var contract = await store.GetAsync(command.ContractId, true, cancellationToken);
        if (contract is null || contract.OrganizationId != command.OrganizationId)
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Technical contract not found.", 404);
        try
        {
            contract.ReplaceDefinition(ToEffects(command.Effects), ToConstraints(command.OrderingConstraints));
            contract.UpdatedByAccountId = command.UserContext.AccountId;
            await store.SaveChangesAsync(cancellationToken);
            return ApiResult<RobotArtifactTechnicalContractResult>.Success(RobotArtifactTechnicalContractResult.From(contract));
        }
        catch (DomainRuleException ex) { return ApiResult<RobotArtifactTechnicalContractResult>.Fail(ex.Message, 400); }
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
            contract.ValidateForPublication(parameterizedRuntimeSupported: false);
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
        var contract = await store.GetAsync(command.ContractId, true, cancellationToken);
        if (contract is null || contract.OrganizationId != command.OrganizationId)
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail("Technical contract not found.", 404);
        if (await store.HasPublishedTemplateReferenceAsync(contract.Id, cancellationToken))
            return ApiResult<RobotArtifactTechnicalContractResult>.Fail(
                "Retire published templates that reference this technical contract before retiring the contract.", 409);
        try
        {
            contract.Retire(DateTimeOffset.UtcNow, command.UserContext.AccountId);
            await store.SaveChangesAsync(cancellationToken);
            return ApiResult<RobotArtifactTechnicalContractResult>.Success(RobotArtifactTechnicalContractResult.From(contract));
        }
        catch (DomainRuleException ex) { return ApiResult<RobotArtifactTechnicalContractResult>.Fail(ex.Message, 400); }
    }

    public async Task<ApiResult<object>> DiscardAsync(
        DiscardRobotArtifactTechnicalContractCommand command, CancellationToken cancellationToken)
    {
        if (!CanManage(command.UserContext, command.OrganizationId))
            return ApiResult<object>.Fail("Access denied.", 403);
        var contract = await store.GetAsync(command.ContractId, true, cancellationToken);
        if (contract is null || contract.OrganizationId != command.OrganizationId)
            return ApiResult<object>.Fail("Technical contract not found.", 404);
        if (contract.Status != RobotArtifactContractStatus.Draft)
            return ApiResult<object>.Fail("Only Draft technical contracts can be discarded.", 409);
        store.Remove(contract);
        await store.SaveChangesAsync(cancellationToken);
        return ApiResult<object>.Success(new { contract.Id }, "Technical contract Draft discarded.");
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
    IRobotArtifactStore artifacts)
{
    public async Task<ApiResult<object>> AssignTemplateAsync(CurrentUserContext user, Guid templateId,
        Guid contractId, CancellationToken cancellationToken)
    {
        if (!user.IsSystemAdmin) return ApiResult<object>.Fail("Access denied.", 403);
        var contract = await contracts.GetAsync(contractId, false, cancellationToken);
        var template = await templates.GetByIdAsync(templateId, true, cancellationToken);
        if (contract is null || template is null || contract.OrganizationId.HasValue ||
            contract.Status != RobotArtifactContractStatus.Published || string.IsNullOrWhiteSpace(contract.ContractChecksum))
            return ApiResult<object>.Fail("Published global technical contract or template not found.", 404);
        if (!string.Equals(contract.RuntimeTargetCode, template.RuntimeTargetCode, StringComparison.Ordinal) ||
            !string.Equals(contract.MachineModelCode, template.MachineModelCode, StringComparison.Ordinal))
            return ApiResult<object>.Fail("Technical contract target does not match artifact template.", 400);
        try
        {
            template.AssignTechnicalContract(contract.Id, contract.ContractChecksum);
            await templates.SaveChangesAsync(cancellationToken);
            return ApiResult<object>.Success(new { template.Id, TechnicalContractId = contract.Id, contract.ContractChecksum });
        }
        catch (DomainRuleException ex) { return ApiResult<object>.Fail(ex.Message, 400); }
    }

    public async Task<ApiResult<object>> AssignArtifactAsync(CurrentUserContext user, Guid organizationId,
        Guid artifactId, Guid contractId, CancellationToken cancellationToken)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactUpload, user, organizationId, null, null))
            return ApiResult<object>.Fail("Access denied.", 403);
        var contract = await contracts.GetAsync(contractId, false, cancellationToken);
        var artifact = await artifacts.GetArtifactForPublishAsync(organizationId, artifactId, cancellationToken);
        if (contract is null || artifact is null || contract.Status != RobotArtifactContractStatus.Published ||
            (contract.OrganizationId.HasValue && contract.OrganizationId != organizationId) || string.IsNullOrWhiteSpace(contract.ContractChecksum))
            return ApiResult<object>.Fail("Published technical contract or Draft artifact not found.", 404);
        if (!string.Equals(contract.RuntimeTargetCode, artifact.RuntimeTargetCode, StringComparison.Ordinal) ||
            !string.Equals(contract.MachineModelCode, artifact.MachineModelCode, StringComparison.Ordinal))
            return ApiResult<object>.Fail("Technical contract target does not match artifact.", 400);
        try
        {
            artifact.AssignTechnicalContract(contract.Id, contract.ContractChecksum);
            await artifacts.SaveChangesAsync(cancellationToken);
            return ApiResult<object>.Success(new { artifact.Id, TechnicalContractId = contract.Id, contract.ContractChecksum });
        }
        catch (DomainRuleException ex) { return ApiResult<object>.Fail(ex.Message, 400); }
    }
}
