using Application.Operations.OperationLogs.Abstractions;
using Application.Operations.OperationLogs.Mapping;
using Application.Operations.OperationLogs.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Application.Tenants.Kiosks;

namespace Application.Operations.OperationLogs.Queries;

public sealed class GetOperationLogQueryHandler
{
    private readonly IOperationLogStore _store;

    public GetOperationLogQueryHandler(IOperationLogStore store)
    {
        _store = store;
    }

    public async Task<ApiResult<OperationLogResult>> HandleAsync(
        GetOperationLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await _store.GetKioskByIdAsync(query.KioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<OperationLogResult>.Fail("Kiosk not found.", 404);
        }

        if (!KioskAccessRules.CanAccessKiosk(ScopeRoleSets.OperationsView, query.UserContext, kiosk))
        {
            return ApiResult<OperationLogResult>.Fail("Access denied.", 403);
        }

        var log = await _store.GetByKioskIdAsync(query.KioskId, query.OperationLogId, cancellationToken);
        if (log is null)
        {
            return ApiResult<OperationLogResult>.Fail("Operation log not found.", 404);
        }

        return ApiResult<OperationLogResult>.Success(OperationLogResultMapper.ToResult(log));
    }
}
