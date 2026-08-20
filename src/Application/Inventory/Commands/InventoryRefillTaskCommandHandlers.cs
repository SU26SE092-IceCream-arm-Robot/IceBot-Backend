using System.Security.Cryptography;
using System.Text;
using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;

namespace Application.Inventory.Commands;

public sealed class RequestInventoryRefillTaskCommandHandler(IInventoryStore inventory)
{
    public async Task<ApiResult<InventoryRefillTaskResult>> HandleAsync(RequestInventoryRefillTaskCommand command, CancellationToken cancellationToken = default)
    {
        if (InventoryRefillTaskCommandValidation.ValidateRequest(command) is { } validationError)
            return ApiResult<InventoryRefillTaskResult>.Fail(validationError, 400);
        var fingerprint = RefillTaskRequestFingerprint.Create("request", command.InventoryId, command.RequestedQuantity, command.IngredientDispenserStateId, command.ReasonCode, command.Notes);
        try
        {
            return await inventory.ExecuteInTransactionAsync(async ct =>
            {
                var replay = await inventory.GetInventoryRefillTaskByRequestKeyAsync(command.KioskId, command.IdempotencyKey.Trim(), ct);
                if (replay is not null)
                    return replay.RequestFingerprint == fingerprint
                        ? ApiResult<InventoryRefillTaskResult>.Success(InventoryRefillTaskResultMapper.ToResult(replay))
                        : ApiResult<InventoryRefillTaskResult>.Fail("Idempotency key was already used with a different request.", 409);

                await inventory.AcquireKioskIngredientInventoryMutationLockAsync(command.InventoryId, ct);
                var balance = await inventory.GetKioskIngredientInventoryAsync(command.InventoryId, ct);
                if (balance is null || balance.KioskId != command.KioskId) return ApiResult<InventoryRefillTaskResult>.Fail("Kiosk inventory was not found.", 404);
                if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryRefillManage, command.UserContext, balance.OrganizationId, balance.StoreId, balance.KioskId)) return ApiResult<InventoryRefillTaskResult>.Fail("Access denied.", 403);

                replay = await inventory.GetInventoryRefillTaskByRequestKeyAsync(command.KioskId, command.IdempotencyKey.Trim(), ct);
                if (replay is not null)
                    return replay.RequestFingerprint == fingerprint
                        ? ApiResult<InventoryRefillTaskResult>.Success(InventoryRefillTaskResultMapper.ToResult(replay))
                        : ApiResult<InventoryRefillTaskResult>.Fail("Idempotency key was already used with a different request.", 409);

                if (await inventory.GetActiveInventoryRefillTaskAsync(balance.Id, ct) is { } active)
                    return ApiResult<InventoryRefillTaskResult>.Fail("An active refill task already exists.", 409).AddDetail("taskId", active.Id);
                if (command.IngredientDispenserStateId.HasValue &&
                    !await InventoryRefillTaskCommandSupport.IsValidEvidenceAsync(
                        inventory,
                        command.IngredientDispenserStateId.Value,
                        balance,
                        ct))
                {
                    return ApiResult<InventoryRefillTaskResult>.Fail(
                        "Dispenser evidence is not active or does not bind to this balance.",
                        409);
                }

                var now = DateTimeOffset.UtcNow;
                var task = new InventoryRefillTask
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = balance.OrganizationId,
                    StoreId = balance.StoreId,
                    KioskId = balance.KioskId,
                    KioskIngredientInventoryId = balance.Id,
                    IngredientDispenserStateId = command.IngredientDispenserStateId,
                    RequestSource = command.RequestSource,
                    RequestedQuantity = command.RequestedQuantity,
                    Unit = balance.Unit,
                    RequestedAt = now,
                    RequestedByAccountId = command.UserContext.AccountId,
                    RequestIdempotencyKey = command.IdempotencyKey.Trim(),
                    RequestFingerprint = fingerprint,
                    CreatedAt = now,
                    CreatedByAccountId = command.UserContext.AccountId,
                    Version = 1
                };
                await inventory.AddInventoryRefillTaskAsync(task, ct);
                await InventoryRefillTaskCommandSupport.AddTransitionAsync(
                    inventory, task, null, InventoryRefillTaskStatus.Requested, command.UserContext,
                    command.IdempotencyKey, fingerprint, command.ReasonCode, null, now, ct);
                await inventory.SaveChangesAsync(ct);
                return ApiResult<InventoryRefillTaskResult>.Success(InventoryRefillTaskResultMapper.ToResult(task), statusCode: 201);
            }, cancellationToken);
        }
        catch (DomainRuleException ex) { return ApiResult<InventoryRefillTaskResult>.Fail(ex.Message, 409); }
    }

}

public sealed class StartInventoryRefillTaskCommandHandler(IInventoryStore inventory)
{
    public Task<ApiResult<InventoryRefillTaskResult>> HandleAsync(StartInventoryRefillTaskCommand command, CancellationToken cancellationToken = default) =>
        TransitionAsync(command.KioskId, command.TaskId, command.IdempotencyKey, command.UserContext, InventoryRefillTaskStatus.InProgress, null, cancellationToken);

    private async Task<ApiResult<InventoryRefillTaskResult>> TransitionAsync(Guid kioskId, Guid taskId, string key, Application.Identity.Tokens.Claims.CurrentUserContext user, InventoryRefillTaskStatus target, string? reason, CancellationToken ct)
    {
        if (InventoryRefillTaskCommandValidation.ValidateIdempotencyKey(key) is { } validationError)
            return ApiResult<InventoryRefillTaskResult>.Fail(validationError, 400);
        var fingerprint = RefillTaskRequestFingerprint.Create(target.ToString(), taskId, null, null, reason, null);
        try
        {
            return await inventory.ExecuteInTransactionAsync(async token =>
        {
            var task = await inventory.GetInventoryRefillTaskAsync(taskId, token);
            if (task is null || task.KioskId != kioskId) return ApiResult<InventoryRefillTaskResult>.Fail("Inventory refill task was not found.", 404);
            await inventory.AcquireKioskIngredientInventoryMutationLockAsync(task.KioskIngredientInventoryId, token);
            await inventory.AcquireInventoryRefillTaskMutationLockAsync(task.Id, token);
            task = await inventory.GetInventoryRefillTaskAsync(task.Id, token);
            if (task is null) return ApiResult<InventoryRefillTaskResult>.Fail("Inventory refill task was not found.", 404);
            if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryRefillManage, user, task.OrganizationId, task.StoreId, task.KioskId)) return ApiResult<InventoryRefillTaskResult>.Fail("Access denied.", 403);
            var replay = await inventory.GetInventoryRefillTaskTransitionByRequestKeyAsync(task.Id, key.Trim(), token);
            if (replay is not null) return replay.RequestFingerprint == fingerprint ? ApiResult<InventoryRefillTaskResult>.Success(InventoryRefillTaskResultMapper.ToResult(task)) : ApiResult<InventoryRefillTaskResult>.Fail("Idempotency key was already used with a different request.", 409);
            var now = DateTimeOffset.UtcNow;
            var from = task.Status;
            task.Start(user.AccountId, now);
            await InventoryRefillTaskCommandSupport.AddTransitionAsync(
                inventory, task, from, target, user, key, fingerprint, reason, null, now, token);
            await inventory.SaveChangesAsync(token);
            return ApiResult<InventoryRefillTaskResult>.Success(InventoryRefillTaskResultMapper.ToResult(task));
        }, ct);
        }
        catch (DomainRuleException ex) { return ApiResult<InventoryRefillTaskResult>.Fail(ex.Message, 409); }
    }
}

public sealed class CompleteInventoryRefillTaskCommandHandler(IInventoryStore inventory)
{
    public async Task<ApiResult<InventoryRefillTaskResult>> HandleAsync(CompleteInventoryRefillTaskCommand command, CancellationToken cancellationToken = default)
    {
        if (InventoryRefillTaskCommandValidation.ValidateCompletion(command) is { } validationError)
            return ApiResult<InventoryRefillTaskResult>.Fail(validationError, 400);
        var fingerprint = RefillTaskRequestFingerprint.Create("complete", command.TaskId, command.ActualQuantity, command.IngredientDispenserStateId, command.ReasonCode, command.Notes, command.ExternalLotReference);
        try
        {
            return await inventory.ExecuteInTransactionAsync(async ct =>
        {
            var task = await inventory.GetInventoryRefillTaskAsync(command.TaskId, ct);
            if (task is null || task.KioskId != command.KioskId) return ApiResult<InventoryRefillTaskResult>.Fail("Inventory refill task was not found.", 404);
            await inventory.AcquireKioskIngredientInventoryMutationLockAsync(task.KioskIngredientInventoryId, ct);
            await inventory.AcquireInventoryRefillTaskMutationLockAsync(task.Id, ct);
            task = await inventory.GetInventoryRefillTaskAsync(task.Id, ct);
            if (task is null) return ApiResult<InventoryRefillTaskResult>.Fail("Inventory refill task was not found.", 404);
            var balance = await inventory.GetKioskIngredientInventoryAsync(task.KioskIngredientInventoryId, ct);
            if (balance is null) return ApiResult<InventoryRefillTaskResult>.Fail("Kiosk inventory was not found.", 404);
            if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryRefillManage, command.UserContext, balance.OrganizationId, balance.StoreId, balance.KioskId)) return ApiResult<InventoryRefillTaskResult>.Fail("Access denied.", 403);
            var replay = await inventory.GetInventoryRefillTaskTransitionByRequestKeyAsync(task.Id, command.IdempotencyKey.Trim(), ct);
            if (replay is not null) return replay.RequestFingerprint == fingerprint ? ApiResult<InventoryRefillTaskResult>.Success(InventoryRefillTaskResultMapper.ToResult(task)) : ApiResult<InventoryRefillTaskResult>.Fail("Idempotency key was already used with a different request.", 409);
            var evidenceId = command.IngredientDispenserStateId ?? task.IngredientDispenserStateId;
            if (evidenceId.HasValue &&
                !await InventoryRefillTaskCommandSupport.IsValidEvidenceAsync(
                    inventory,
                    evidenceId.Value,
                    balance,
                    ct))
            {
                return ApiResult<InventoryRefillTaskResult>.Fail(
                    "Dispenser evidence is not active or does not bind to this balance.",
                    409);
            }
            var now = DateTimeOffset.UtcNow;
            var before = balance.EstimatedQuantity;
            var from = task.Status;
            balance.Refill(command.ActualQuantity, now);
            task.Complete(command.ActualQuantity, command.ReasonCode, command.Notes, command.ExternalLotReference, command.UserContext.AccountId, now);
            var movement = StockMovement.CreateForKioskInventory(balance.Id, balance.OrganizationId, balance.StoreId, balance.KioskId, balance.IngredientId, "ManualRefill", command.ActualQuantity, before, balance.EstimatedQuantity, balance.Unit, now, command.ReasonCode, "InventoryRefillTask", task.Id, isEstimated: true);
            movement.CreatedByAccountId = command.UserContext.AccountId;
            await inventory.AddStockMovementAsync(movement, ct);
            if (balance.TrackingMode is InventoryTrackingMode.SensorAssisted or InventoryTrackingMode.SensorRequired)
            {
                IEnumerable<IngredientDispenserState> states = evidenceId.HasValue
                    ? (await inventory.GetDispenserStateByIdAsync(evidenceId.Value, ct) is { } evidenceState ? [evidenceState] : [])
                    : await inventory.ListBoundDispenserStatesForMutationAsync(balance.Id, ct);
                foreach (var state in states) state.RequireSensorRebaseline(task.Id, now);
            }
            if (task.SourceAlertId.HasValue)
            {
                await inventory.AcquireAlertMutationLockAsync(task.SourceAlertId.Value, ct);
                var sourceAlert = await inventory.GetAlertByIdAsync(task.SourceAlertId.Value, ct);
                if (sourceAlert is not null &&
                    sourceAlert.Status is not Domain.Operations.Enums.AlertStatus.Resolved and not Domain.Operations.Enums.AlertStatus.Suppressed &&
                    IsRecoveredAboveThreshold(balance))
                    sourceAlert.Resolve(now, "Inventory refill completed.");
            }
            await InventoryRefillTaskCommandSupport.AddTransitionAsync(
                inventory, task, from, InventoryRefillTaskStatus.Completed, command.UserContext,
                command.IdempotencyKey, fingerprint, command.ReasonCode, command.ActualQuantity, now, ct);
            await inventory.SaveChangesAsync(ct);
            return ApiResult<InventoryRefillTaskResult>.Success(InventoryRefillTaskResultMapper.ToResult(task));
        }, cancellationToken);
        }
        catch (DomainRuleException ex) { return ApiResult<InventoryRefillTaskResult>.Fail(ex.Message, 409); }
    }

    private static bool IsRecoveredAboveThreshold(KioskIngredientInventory balance) =>
        balance.EstimatedQuantity is > 0 &&
        (!balance.LowStockThreshold.HasValue || balance.EstimatedQuantity > balance.LowStockThreshold);
}

public sealed class CancelInventoryRefillTaskCommandHandler(IInventoryStore inventory)
{
    public async Task<ApiResult<InventoryRefillTaskResult>> HandleAsync(CancelInventoryRefillTaskCommand command, CancellationToken cancellationToken = default)
    {
        if (InventoryRefillTaskCommandValidation.ValidateCancellation(command) is { } validationError)
            return ApiResult<InventoryRefillTaskResult>.Fail(validationError, 400);
        var fingerprint = RefillTaskRequestFingerprint.Create("cancel", command.TaskId, null, null, command.Reason, null);
        try
        {
            return await inventory.ExecuteInTransactionAsync(async ct =>
        {
            var task = await inventory.GetInventoryRefillTaskAsync(command.TaskId, ct);
            if (task is null || task.KioskId != command.KioskId) return ApiResult<InventoryRefillTaskResult>.Fail("Inventory refill task was not found.", 404);
            await inventory.AcquireKioskIngredientInventoryMutationLockAsync(task.KioskIngredientInventoryId, ct);
            await inventory.AcquireInventoryRefillTaskMutationLockAsync(task.Id, ct);
            task = await inventory.GetInventoryRefillTaskAsync(task.Id, ct);
            if (task is null) return ApiResult<InventoryRefillTaskResult>.Fail("Inventory refill task was not found.", 404);
            if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryRefillManage, command.UserContext, task.OrganizationId, task.StoreId, task.KioskId)) return ApiResult<InventoryRefillTaskResult>.Fail("Access denied.", 403);
            var replay = await inventory.GetInventoryRefillTaskTransitionByRequestKeyAsync(task.Id, command.IdempotencyKey.Trim(), ct);
            if (replay is not null) return replay.RequestFingerprint == fingerprint ? ApiResult<InventoryRefillTaskResult>.Success(InventoryRefillTaskResultMapper.ToResult(task)) : ApiResult<InventoryRefillTaskResult>.Fail("Idempotency key was already used with a different request.", 409);
            var now = DateTimeOffset.UtcNow;
            var from = task.Status;
            task.Cancel(command.Reason, command.UserContext.AccountId, now);
            await InventoryRefillTaskCommandSupport.AddTransitionAsync(
                inventory, task, from, InventoryRefillTaskStatus.Cancelled, command.UserContext,
                command.IdempotencyKey, fingerprint, command.Reason, null, now, ct);
            await inventory.SaveChangesAsync(ct);
            return ApiResult<InventoryRefillTaskResult>.Success(InventoryRefillTaskResultMapper.ToResult(task));
        }, cancellationToken);
        }
        catch (DomainRuleException ex) { return ApiResult<InventoryRefillTaskResult>.Fail(ex.Message, 409); }
    }
}

internal static class InventoryRefillTaskCommandSupport
{
    public static async Task<bool> IsValidEvidenceAsync(
        IInventoryStore inventory,
        Guid dispenserStateId,
        KioskIngredientInventory balance,
        CancellationToken cancellationToken)
    {
        var state = await inventory.GetDispenserStateByIdAsync(dispenserStateId, cancellationToken);
        return state is not null &&
               state.IsActive &&
               state.KioskId == balance.KioskId &&
               state.KioskIngredientInventoryId == balance.Id;
    }

    public static async Task AddTransitionAsync(
        IInventoryStore inventory,
        InventoryRefillTask task,
        InventoryRefillTaskStatus? from,
        InventoryRefillTaskStatus to,
        Application.Identity.Tokens.Claims.CurrentUserContext user,
        string key,
        string fingerprint,
        string? reason,
        decimal? quantity,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var scope = ScopeAccessRules.GetAuthorizingScopeSnapshots(
                ScopeRoleSets.InventoryRefillManage,
                user,
                task.OrganizationId,
                task.StoreId,
                task.KioskId)
            .FirstOrDefault();

        await inventory.AddInventoryRefillTaskTransitionAsync(new InventoryRefillTaskTransition
        {
            Id = Guid.NewGuid(),
            InventoryRefillTaskId = task.Id,
            FromStatus = from,
            ToStatus = to,
            ActorAccountId = user.AccountId,
            ActorRoleCode = scope?.RoleCode,
            ActorOrganizationId = scope?.OrganizationId,
            ActorStoreId = scope?.StoreId,
            ActorKioskId = scope?.KioskId,
            Reason = reason,
            ActualQuantity = quantity,
            RequestIdempotencyKey = key.Trim(),
            RequestFingerprint = fingerprint,
            OccurredAt = now,
            CreatedAt = now,
            CreatedByAccountId = user.AccountId
        }, cancellationToken);
    }
}

internal static class RefillTaskRequestFingerprint
{
    public static string Create(string operation, Guid id, decimal? quantity, Guid? dispenserId, params string?[] text)
    {
        var raw = string.Join('|', [operation, id.ToString("N"), quantity?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty, dispenserId?.ToString("N") ?? string.Empty, .. text.Select(value => value?.Trim() ?? string.Empty)]);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}

internal static class InventoryRefillTaskCommandValidation
{
    private const int IdempotencyKeyMaxLength = 200;
    private const int ReasonCodeMaxLength = 100;
    private const int NotesMaxLength = 1_000;
    private const int ExternalLotReferenceMaxLength = 200;

    public static string? ValidateRequest(RequestInventoryRefillTaskCommand command)
    {
        if (ValidateIdempotencyKey(command.IdempotencyKey) is { } idempotencyError) return idempotencyError;
        if (command.RequestedQuantity is <= 0) return "Requested refill quantity must be greater than zero when supplied.";
        return ValidateText(command.ReasonCode, ReasonCodeMaxLength, "Reason code")
            ?? ValidateText(command.Notes, NotesMaxLength, "Notes");
    }

    public static string? ValidateCompletion(CompleteInventoryRefillTaskCommand command)
    {
        if (ValidateIdempotencyKey(command.IdempotencyKey) is { } idempotencyError) return idempotencyError;
        if (command.ActualQuantity <= 0) return "Actual refill quantity must be greater than zero.";
        return ValidateText(command.ReasonCode, ReasonCodeMaxLength, "Reason code")
            ?? ValidateText(command.Notes, NotesMaxLength, "Notes")
            ?? ValidateText(command.ExternalLotReference, ExternalLotReferenceMaxLength, "External lot reference");
    }

    public static string? ValidateCancellation(CancelInventoryRefillTaskCommand command) =>
        ValidateIdempotencyKey(command.IdempotencyKey)
        ?? (string.IsNullOrWhiteSpace(command.Reason) ? "Cancellation reason is required." : null)
        ?? ValidateText(command.Reason, NotesMaxLength, "Cancellation reason");

    public static string? ValidateIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "Idempotency-Key header is required.";
        return key.Trim().Length > IdempotencyKeyMaxLength ? "Idempotency-Key header must not exceed 200 characters." : null;
    }

    private static string? ValidateText(string? value, int maxLength, string fieldName) =>
        value?.Trim().Length > maxLength ? $"{fieldName} must not exceed {maxLength} characters." : null;
}
