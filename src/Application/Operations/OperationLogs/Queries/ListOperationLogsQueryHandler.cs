using Application.Operations.OperationLogs.Abstractions;
using Application.Operations.OperationLogs.Mapping;
using Application.Operations.OperationLogs.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Application.Tenants.Kiosks;
using Domain.Common.Enums;

namespace Application.Operations.OperationLogs.Queries;

public sealed class ListOperationLogsQueryHandler
{
    private readonly IOperationLogStore _store;

    public ListOperationLogsQueryHandler(IOperationLogStore store)
    {
        _store = store;
    }

    public async Task<PagedResult<OperationLogResult>> HandleAsync(
        ListOperationLogsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        if (!TryParseSeverity(query.Severity, out var severity))
        {
            return PagedResult<OperationLogResult>.Fail("Invalid operation-log severity.", 400, pageNumber, pageSize);
        }

        if (query.From.HasValue && query.To.HasValue && query.From > query.To)
        {
            return PagedResult<OperationLogResult>.Fail("Operation-log from timestamp cannot be after to timestamp.", 400, pageNumber, pageSize);
        }

        var user = query.UserContext;
        var kiosk = await _store.GetKioskByIdAsync(query.KioskId, cancellationToken);
        if (kiosk is null)
        {
            return PagedResult<OperationLogResult>.Fail("Kiosk not found.", 404, pageNumber, pageSize);
        }

        if (!KioskAccessRules.CanAccessKiosk(ScopeRoleSets.OperationsView, user, kiosk))
        {
            return PagedResult<OperationLogResult>.Forbidden("Access denied.", pageNumber, pageSize);
        }

        var total = await _store.CountAsync(
            query.KioskId, query.DeviceId, query.OrderId, severity, query.From, query.To, cancellationToken);
        var logs = await _store.ListAsync(
            query.KioskId, query.DeviceId, query.OrderId, severity, query.From, query.To,
            pageNumber, pageSize, cancellationToken);

        return PagedResult<OperationLogResult>.Success(
            logs.Select(OperationLogResultMapper.ToResult).ToList(), total, pageNumber, pageSize);
    }

    private static bool TryParseSeverity(string? value, out SeverityLevel? severity)
    {
        severity = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Enum.TryParse<SeverityLevel>(value.Trim(), true, out var parsed) || !Enum.IsDefined(parsed))
        {
            return false;
        }

        severity = parsed;
        return true;
    }
}
