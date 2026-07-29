using Application.RobotConfiguration.Programs.Abstractions;
using Application.RobotConfiguration.Programs.ReadModels;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.RobotConfiguration.Programs.Queries;

public sealed class ListRobotProgramsQueryHandler
{
    private readonly IRobotProgramStore _store;

    public ListRobotProgramsQueryHandler(IRobotProgramStore store) => _store = store;

    public async Task<PagedResult<RobotProgramSummaryReadModel>> HandleAsync(
        ListRobotProgramsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var user = query.UserContext;
        if (query.OrganizationId == Guid.Empty)
        {
            return PagedResult<RobotProgramSummaryReadModel>.Fail(
                "Organization is required.", 400, pageNumber, pageSize);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.ProgramRead, user, query.OrganizationId, null, null))
        {
            return PagedResult<RobotProgramSummaryReadModel>.Fail(
                "Access denied.", 403, pageNumber, pageSize);
        }

        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.ProgramRead, user);
        var count = await _store.CountProgramsAsync(
            query.OrganizationId, query.Search, query.Status, user.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds, cancellationToken);
        var programs = await _store.ListProgramsAsync(
            query.OrganizationId, query.Search, query.Status, user.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds,
            pageNumber, pageSize, cancellationToken);

        return PagedResult<RobotProgramSummaryReadModel>.Success(
            programs, count, pageNumber, pageSize);
    }
}
