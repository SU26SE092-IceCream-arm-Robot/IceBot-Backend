using Domain.Sync.DeadLetters;
using Domain.Sync.Ingestion;
using System.Text.Json;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Sync.Abstractions;
using Application.Sync.Results;
using Domain.Common;
using Domain.Sync.Entities;
using Domain.Sync.Enums;

namespace Application.Sync;

public sealed record ListSyncDeadLettersQuery(CurrentUserContext UserContext, string? Status, string? EventType, int PageNumber, int PageSize);
public sealed record GetSyncDeadLetterQuery(CurrentUserContext UserContext, Guid Id);
public sealed record ResolveSyncDeadLetterCommand(CurrentUserContext UserContext, Guid Id, string Notes);
public sealed record IgnoreSyncDeadLetterCommand(CurrentUserContext UserContext, Guid Id, string Reason);
public sealed record RetrySyncDeadLetterCommand(CurrentUserContext UserContext, Guid Id, string Reason);

public sealed class ListSyncDeadLettersQueryHandler
{
    private readonly ISyncDeadLetterStore _store;
    public ListSyncDeadLettersQueryHandler(ISyncDeadLetterStore store) => _store = store;
    public async Task<PagedResult<SyncDeadLetterResult>> HandleAsync(ListSyncDeadLettersQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.PageNumber); var size = Math.Clamp(query.PageSize, 1, 100);
        var (items, total) = await _store.ListAsync(query.Status, query.EventType, page, size, ct);
        return PagedResult<SyncDeadLetterResult>.Success(items.Select(Map), total, page, size);
    }
    internal static SyncDeadLetterResult Map(SyncDeadLetter x) => new()
    {
        Id = x.Id, EventId = x.EventId, KioskId = x.KioskId, KioskCode = x.Kiosk?.Code,
        EventType = x.EventType, AggregateType = x.AggregateType, AggregateId = x.AggregateId,
        Status = x.Status.ToString(), ProcessingAttempts = x.ProcessingAttempts,
        ErrorMessage = x.ErrorMessage, FailedAt = x.FailedAt, ResolvedAt = x.ResolvedAt,
        ResolutionNotes = x.ResolutionNotes,
        RetryAttempts = x.RetryAttempts.OrderByDescending(a => a.AttemptNumber).Select(a => new SyncDeadLetterRetryAttemptResult
        {
            AttemptNumber = a.AttemptNumber, RequestedByAccountId = a.RequestedByAccountId,
            RequestedAt = a.RequestedAt, Reason = a.Reason, Succeeded = a.Succeeded,
            CompletedAt = a.CompletedAt, ResultMessage = a.ResultMessage
        }).ToArray()
    };
}

public sealed class GetSyncDeadLetterQueryHandler
{
    private readonly ISyncDeadLetterStore _store;
    public GetSyncDeadLetterQueryHandler(ISyncDeadLetterStore store) => _store = store;
    public async Task<ApiResult<SyncDeadLetterResult>> HandleAsync(GetSyncDeadLetterQuery query, CancellationToken ct = default)
    {
        var item = await _store.GetAsync(query.Id, false, ct);
        return item is null ? ApiResult<SyncDeadLetterResult>.Fail("Sync dead letter not found.", 404)
            : ApiResult<SyncDeadLetterResult>.Success(ListSyncDeadLettersQueryHandler.Map(item));
    }
}

public sealed class ResolveSyncDeadLetterCommandHandler
{
    private readonly ISyncDeadLetterStore _store;
    public ResolveSyncDeadLetterCommandHandler(ISyncDeadLetterStore store) => _store = store;
    public Task<ApiResult<SyncDeadLetterResult>> HandleAsync(ResolveSyncDeadLetterCommand command, CancellationToken ct = default) =>
        CompleteAsync(command.Id, command.UserContext.AccountId, command.Notes, false, ct);
    private async Task<ApiResult<SyncDeadLetterResult>> CompleteAsync(Guid id, Guid actor, string reason, bool ignore, CancellationToken ct)
    {
        var item = await _store.GetAsync(id, true, ct);
        if (item is null) return ApiResult<SyncDeadLetterResult>.Fail("Sync dead letter not found.", 404);
        try
        {
            if (ignore) item.Ignore(actor, DateTimeOffset.UtcNow, reason); else item.Resolve(actor, DateTimeOffset.UtcNow, reason);
            await _store.SaveChangesAsync(ct);
            return ApiResult<SyncDeadLetterResult>.Success(ListSyncDeadLettersQueryHandler.Map(item));
        }
        catch (DomainRuleException ex) { return ApiResult<SyncDeadLetterResult>.Fail(ex.Message, 400); }
    }
}

public sealed class IgnoreSyncDeadLetterCommandHandler
{
    private readonly ISyncDeadLetterStore _store;
    public IgnoreSyncDeadLetterCommandHandler(ISyncDeadLetterStore store) => _store = store;
    public async Task<ApiResult<SyncDeadLetterResult>> HandleAsync(IgnoreSyncDeadLetterCommand command, CancellationToken ct = default)
    {
        var item = await _store.GetAsync(command.Id, true, ct);
        if (item is null) return ApiResult<SyncDeadLetterResult>.Fail("Sync dead letter not found.", 404);
        try { item.Ignore(command.UserContext.AccountId, DateTimeOffset.UtcNow, command.Reason); await _store.SaveChangesAsync(ct); return ApiResult<SyncDeadLetterResult>.Success(ListSyncDeadLettersQueryHandler.Map(item)); }
        catch (DomainRuleException ex) { return ApiResult<SyncDeadLetterResult>.Fail(ex.Message, 400); }
    }
}

public sealed class RetrySyncDeadLetterCommandHandler
{
    private readonly ISyncDeadLetterStore _store;
    private readonly IngestExecutionReportCommandHandler _executionReportHandler;
    public RetrySyncDeadLetterCommandHandler(ISyncDeadLetterStore store, IngestExecutionReportCommandHandler executionReportHandler)
    { _store = store; _executionReportHandler = executionReportHandler; }

    public async Task<ApiResult<SyncDeadLetterResult>> HandleAsync(RetrySyncDeadLetterCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Reason)) return ApiResult<SyncDeadLetterResult>.Fail("Retry reason is required.", 400);
        var item = await _store.GetAsync(command.Id, true, ct);
        if (item is null) return ApiResult<SyncDeadLetterResult>.Fail("Sync dead letter not found.", 404);
        if (!item.EventType.StartsWith("ExecutionReport.", StringComparison.Ordinal) || item.SyncEventInbox is null || !item.SourceNodeId.HasValue || !item.KioskId.HasValue)
            return ApiResult<SyncDeadLetterResult>.Fail("This dead-letter event type has no registered replay contract.", 422);
        var endpoint = await _store.GetEndpointBySourceNodeAsync(item.SourceNodeId.Value, ct);
        if (endpoint is null) return ApiResult<SyncDeadLetterResult>.Fail("Execution endpoint for dead-letter source was not found.", 409);

        ExecutionReportReplayPayload? payload;
        try { payload = JsonSerializer.Deserialize<ExecutionReportReplayPayload>(item.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch (JsonException) { return ApiResult<SyncDeadLetterResult>.Fail("Dead-letter payload is not a valid execution report.", 422); }
        if (payload is null || !payload.CommandId.HasValue || !item.EventId.HasValue) return ApiResult<SyncDeadLetterResult>.Fail("Dead-letter execution report payload is incomplete.", 422);

        var attempt = SyncDeadLetterRetryAttempt.Create(item.Id, await _store.GetNextRetryAttemptNumberAsync(item.Id, ct), command.UserContext.AccountId, DateTimeOffset.UtcNow, command.Reason);
        item.BeginRetry(); item.SyncEventInbox.PrepareManualRetry(DateTimeOffset.UtcNow);
        await _store.AddRetryAttemptAsync(attempt, ct); await _store.SaveChangesAsync(ct);

        var result = await _executionReportHandler.HandleAsync(payload.ToCommand(item.KioskId.Value, endpoint.Id, item.EventId.Value), ct);
        attempt.Complete(result.Succeeded, DateTimeOffset.UtcNow, result.Message ?? "No retry result message.");
        if (result.Succeeded) item.Resolve(command.UserContext.AccountId, DateTimeOffset.UtcNow, $"Retry succeeded: {command.Reason.Trim()}");
        else item.ReturnToOpen(result.Message ?? "Execution report retry failed.");
        await _store.SaveChangesAsync(ct);
        return result.Succeeded ? ApiResult<SyncDeadLetterResult>.Success(ListSyncDeadLettersQueryHandler.Map(item), "Sync dead letter retried successfully.")
            : ApiResult<SyncDeadLetterResult>.Fail($"Sync retry failed: {result.Message}", result.StatusCode);
    }

    private sealed class ExecutionReportReplayPayload
    {
        public Guid? CommandId { get; init; } public string ReportType { get; init; } = ""; public string Status { get; init; } = "";
        public long SequenceNumber { get; init; } public DateTimeOffset EdgeCreatedAt { get; init; } public DateTimeOffset? ExecutorReportedAt { get; init; }
        public Guid? DeploymentId { get; init; } public Guid? SourceProductionJobId { get; init; } public Guid? OrderItemId { get; init; }
        public int? ProductionUnitNo { get; init; } public int? ProductionUnitQuantity { get; init; } public Guid? WorkcellId { get; init; } public Guid? ControllerId { get; init; }
        public string? ExecutionPlanChecksum { get; init; } public long? ActiveSetVersion { get; init; } public string? ActiveSetChecksum { get; init; }
        public Guid? SourceConfigurationReleaseId { get; init; } public string? ReleaseChecksum { get; init; } public bool? PhysicalOutputMayHaveOccurred { get; init; }
        public string? ErrorCode { get; init; } public string? ErrorMessage { get; init; } public string? PayloadJson { get; init; }
        public IReadOnlyCollection<StockMovementEvidenceInput> StockMovements { get; init; } = [];
        public IngestExecutionReportCommand ToCommand(Guid kioskId, Guid endpointId, Guid eventId) => new()
        {
            KioskId = kioskId, EndpointId = endpointId, CommandId = CommandId!.Value, SourceEventId = eventId,
            SequenceNumber = SequenceNumber, EdgeCreatedAt = EdgeCreatedAt, ExecutorReportedAt = ExecutorReportedAt,
            ReportType = ReportType, Status = Status, DeploymentId = DeploymentId, SourceProductionJobId = SourceProductionJobId,
            OrderItemId = OrderItemId, ProductionUnitNo = ProductionUnitNo, ProductionUnitQuantity = ProductionUnitQuantity,
            WorkcellId = WorkcellId, ControllerId = ControllerId, ExecutionPlanChecksum = ExecutionPlanChecksum,
            ActiveSetVersion = ActiveSetVersion, ActiveSetChecksum = ActiveSetChecksum, SourceConfigurationReleaseId = SourceConfigurationReleaseId,
            ReleaseChecksum = ReleaseChecksum, PhysicalOutputMayHaveOccurred = PhysicalOutputMayHaveOccurred, ErrorCode = ErrorCode,
            ErrorMessage = ErrorMessage, PayloadJson = PayloadJson, StockMovements = StockMovements
        };
    }
}
