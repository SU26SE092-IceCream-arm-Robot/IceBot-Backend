using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.ReadModels;
using Application.Shared.Wrappers;

namespace Application.RobotConfiguration.Queries;

public sealed class ListRobotProgramsQueryHandler
{
    private readonly IRobotConfigurationStore _store;

    public ListRobotProgramsQueryHandler(IRobotConfigurationStore store) => _store = store;

    public async Task<PagedResult<RobotProgramSummaryReadModel>> HandleAsync(
        ListRobotProgramsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var user = query.UserContext;
        var count = await _store.CountProgramsAsync(
            query.OrganizationId, query.Search, query.Status, user.IsSystemAdmin,
            user.AllowedOrganizationIds, user.AllowedStoreIds, user.AllowedKioskIds, cancellationToken);
        var programs = await _store.ListProgramsAsync(
            query.OrganizationId, query.Search, query.Status, user.IsSystemAdmin,
            user.AllowedOrganizationIds, user.AllowedStoreIds, user.AllowedKioskIds,
            pageNumber, pageSize, cancellationToken);

        return PagedResult<RobotProgramSummaryReadModel>.Success(
            programs, count, pageNumber, pageSize);
    }
}
