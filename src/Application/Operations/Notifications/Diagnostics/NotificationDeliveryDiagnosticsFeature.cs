using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Operations.Entities;
using Domain.Operations.Enums;

namespace Application.Operations.Notifications.Diagnostics;

public sealed record NotificationDeliveryDiagnosticsResult(
    Guid Id,
    Guid OrganizationId,
    Guid? StoreId,
    Guid? KioskId,
    Guid? SubjectId,
    string NotificationType,
    Guid RecipientAccountId,
    NotificationDeliveryStatus Status,
    int AttemptCount,
    int MaxAttempts,
    DateTimeOffset NextAttemptAt,
    DateTimeOffset? ProcessingStartedAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? DeliveredAt,
    string? LastErrorCode,
    string? LastErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static NotificationDeliveryDiagnosticsResult From(NotificationDelivery delivery) => new(
        delivery.Id, delivery.OrganizationId, delivery.StoreId, delivery.KioskId, delivery.SubjectId,
        delivery.NotificationType, delivery.RecipientAccountId, delivery.Status,
        delivery.AttemptCount, delivery.MaxAttempts, delivery.NextAttemptAt,
        delivery.ProcessingStartedAt, delivery.LastAttemptAt, delivery.DeliveredAt,
        delivery.LastErrorCode, delivery.LastErrorMessage, delivery.CreatedAt, delivery.UpdatedAt);
}

public sealed record NotificationDeliveryOperationalResult(
    Guid Id,
    Guid OrganizationId,
    Guid? StoreId,
    Guid? KioskId,
    Guid? SubjectId,
    string NotificationType,
    Guid RecipientAccountId,
    NotificationDeliveryStatus Status,
    int AttemptCount,
    int MaxAttempts,
    DateTimeOffset NextAttemptAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? DeliveredAt,
    string? LastErrorCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static NotificationDeliveryOperationalResult From(NotificationDelivery delivery) => new(
        delivery.Id, delivery.OrganizationId, delivery.StoreId, delivery.KioskId, delivery.SubjectId,
        delivery.NotificationType, delivery.RecipientAccountId, delivery.Status,
        delivery.AttemptCount, delivery.MaxAttempts, delivery.NextAttemptAt,
        delivery.LastAttemptAt, delivery.DeliveredAt, delivery.LastErrorCode,
        delivery.CreatedAt, delivery.UpdatedAt);
}

public sealed record ListNotificationDeliveriesQuery(
    CurrentUserContext UserContext,
    Guid OrganizationId,
    string? Status,
    string? NotificationType,
    Guid? RecipientAccountId,
    Guid? KioskId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int PageNumber = 1,
    int PageSize = 20);

public interface INotificationDeliveryReadStore
{
    Task<int> CountAsync(Guid organizationId, NotificationDeliveryStatus? status, string? notificationType,
        Guid? recipientAccountId, Guid? kioskId, DateTimeOffset? from, DateTimeOffset? to,
        bool isSystemAdmin, IReadOnlyCollection<Guid> organizationIds, IReadOnlyCollection<Guid> storeIds,
        IReadOnlyCollection<Guid> kioskIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationDelivery>> ListAsync(Guid organizationId,
        NotificationDeliveryStatus? status, string? notificationType, Guid? recipientAccountId, Guid? kioskId,
        DateTimeOffset? from, DateTimeOffset? to, bool isSystemAdmin,
        IReadOnlyCollection<Guid> organizationIds, IReadOnlyCollection<Guid> storeIds,
        IReadOnlyCollection<Guid> kioskIds, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);
    Task<NotificationDelivery?> GetAsync(Guid organizationId, Guid deliveryId, bool isSystemAdmin,
        IReadOnlyCollection<Guid> organizationIds, IReadOnlyCollection<Guid> storeIds,
        IReadOnlyCollection<Guid> kioskIds, CancellationToken cancellationToken = default);
}

public sealed class NotificationDeliveryDiagnosticsService(INotificationDeliveryReadStore store)
{
    public async Task<PagedResult<NotificationDeliveryDiagnosticsResult>> ListAsync(
        ListNotificationDeliveriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        if (query.From > query.To)
            return PagedResult<NotificationDeliveryDiagnosticsResult>.Fail(
                "Notification delivery from timestamp cannot be after to timestamp.", 400, pageNumber, pageSize);
        NotificationDeliveryStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<NotificationDeliveryStatus>(query.Status, true, out var parsed) ||
                !Enum.IsDefined(parsed))
                return PagedResult<NotificationDeliveryDiagnosticsResult>.Fail(
                    "Invalid notification delivery status.", 400, pageNumber, pageSize);
            status = parsed;
        }

        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.OperationsDiagnostics, query.UserContext);
        var total = await store.CountAsync(query.OrganizationId, status, query.NotificationType,
            query.RecipientAccountId, query.KioskId, query.From, query.To, query.UserContext.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds, cancellationToken);
        var rows = await store.ListAsync(query.OrganizationId, status, query.NotificationType,
            query.RecipientAccountId, query.KioskId, query.From, query.To, query.UserContext.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds, pageNumber, pageSize, cancellationToken);
        return PagedResult<NotificationDeliveryDiagnosticsResult>.Success(
            rows.Select(NotificationDeliveryDiagnosticsResult.From), total, pageNumber, pageSize);
    }

    public async Task<ApiResult<NotificationDeliveryDiagnosticsResult>> GetAsync(
        CurrentUserContext user,
        Guid organizationId,
        Guid deliveryId,
        CancellationToken cancellationToken = default)
    {
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.OperationsDiagnostics, user);
        var delivery = await store.GetAsync(organizationId, deliveryId, user.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds, cancellationToken);
        return delivery is null
            ? ApiResult<NotificationDeliveryDiagnosticsResult>.Fail("Notification delivery not found.", 404)
            : ApiResult<NotificationDeliveryDiagnosticsResult>.Success(
                NotificationDeliveryDiagnosticsResult.From(delivery));
    }
}

public sealed class NotificationDeliveryOperationsService(INotificationDeliveryReadStore store)
{
    public async Task<PagedResult<NotificationDeliveryOperationalResult>> ListAsync(
        ListNotificationDeliveriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        if (query.From > query.To)
            return PagedResult<NotificationDeliveryOperationalResult>.Fail(
                "Notification delivery from timestamp cannot be after to timestamp.", 400, pageNumber, pageSize);

        NotificationDeliveryStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<NotificationDeliveryStatus>(query.Status, true, out var parsed) ||
                !Enum.IsDefined(parsed))
                return PagedResult<NotificationDeliveryOperationalResult>.Fail(
                    "Invalid notification delivery status.", 400, pageNumber, pageSize);
            status = parsed;
        }

        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.NotificationsView, query.UserContext);
        var total = await store.CountAsync(query.OrganizationId, status, query.NotificationType,
            query.RecipientAccountId, query.KioskId, query.From, query.To, query.UserContext.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds, cancellationToken);
        var rows = await store.ListAsync(query.OrganizationId, status, query.NotificationType,
            query.RecipientAccountId, query.KioskId, query.From, query.To, query.UserContext.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds, pageNumber, pageSize, cancellationToken);
        return PagedResult<NotificationDeliveryOperationalResult>.Success(
            rows.Select(NotificationDeliveryOperationalResult.From), total, pageNumber, pageSize);
    }

    public async Task<ApiResult<NotificationDeliveryOperationalResult>> GetAsync(
        CurrentUserContext user,
        Guid organizationId,
        Guid deliveryId,
        CancellationToken cancellationToken = default)
    {
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.NotificationsView, user);
        var delivery = await store.GetAsync(organizationId, deliveryId, user.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds, cancellationToken);
        return delivery is null
            ? ApiResult<NotificationDeliveryOperationalResult>.Fail("Notification delivery not found.", 404)
            : ApiResult<NotificationDeliveryOperationalResult>.Success(
                NotificationDeliveryOperationalResult.From(delivery));
    }
}
