using Application.Operations.OperationLogs.Abstractions;
using Application.Operations.OperationLogs.Mapping;
using Application.Operations.OperationLogs.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Application.Tenants.Kiosks;

namespace Application.Operations.OperationLogs.Queries;

public sealed class GetOperationLogDiagnosticsQueryHandler
{
    private readonly IOperationLogStore _store;

    public GetOperationLogDiagnosticsQueryHandler(IOperationLogStore store)
    {
        _store = store;
    }

    public async Task<ApiResult<OperationLogDiagnosticsResult>> HandleAsync(
        GetOperationLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await _store.GetKioskByIdAsync(query.KioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<OperationLogDiagnosticsResult>.Fail("Kiosk not found.", 404);
        }

        if (!KioskAccessRules.CanAccessKiosk(
                ScopeRoleSets.OperationsDiagnostics, query.UserContext, kiosk))
        {
            return ApiResult<OperationLogDiagnosticsResult>.Fail("Access denied.", 403);
        }

        var log = await _store.GetByKioskIdAsync(query.KioskId, query.OperationLogId, cancellationToken);
        if (log is null)
        {
            return ApiResult<OperationLogDiagnosticsResult>.Fail("Operation log not found.", 404);
        }

        return ApiResult<OperationLogDiagnosticsResult>.Success(OperationLogResultMapper.ToDiagnosticsResult(log));
    }
}
