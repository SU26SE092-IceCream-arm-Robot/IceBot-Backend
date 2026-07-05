using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Identity.Tokens.Claims;
using Application.Operations.Abstractions;
using Application.Operations.MaintenanceTickets.Mapping;
using Application.Operations.MaintenanceTickets.Requests;
using Application.Operations.MaintenanceTickets.Results;
using Application.Operations.MaintenanceTickets.Rules;
using Application.Shared.Wrappers;
using Domain.Operations.Entities;
using Domain.Operations.Enums;

namespace Application.Operations.MaintenanceTickets.Commands;

public sealed class CreateMaintenanceTicketCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required CreateMaintenanceTicketRequest Request { get; init; }
}

public sealed class CreateMaintenanceTicketCommandHandler
{
    private readonly IMaintenanceTicketStore _ticketStore;
    private readonly IRealtimeNotificationPublisher _publisher;

    public CreateMaintenanceTicketCommandHandler(
        IMaintenanceTicketStore ticketStore,
        IRealtimeNotificationPublisher publisher)
    {
        _ticketStore = ticketStore;
        _publisher = publisher;
    }

    public async Task<ApiResult<MaintenanceTicketResult>> HandleAsync(
        CreateMaintenanceTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = command.UserContext;
        var req = command.Request;

        if (string.IsNullOrWhiteSpace(req.Title))
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Title is required.", 400);
        }

        var kioskScope = await _ticketStore.GetKioskScopeAsync(req.KioskId, cancellationToken);
        if (kioskScope is null)
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Kiosk not found.", 404);
        }

        if (!MaintenanceTicketAccessRules.CanCreate(
                user,
                kioskScope.OrganizationId,
                kioskScope.StoreId,
                kioskScope.KioskId))
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Access denied.", 403);
        }

        // 3. Validate optional DeviceId
        if (req.DeviceId.HasValue)
        {
            var isDeviceValid = await _ticketStore.DeviceBelongsToKioskAsync(req.DeviceId.Value, req.KioskId, cancellationToken);
            if (!isDeviceValid)
            {
                return ApiResult<MaintenanceTicketResult>.Fail("The specified device does not belong to this kiosk.", 400);
            }
        }

        // 4. Validate optional OrderId
        if (req.OrderId.HasValue)
        {
            var isOrderValid = await _ticketStore.OrderBelongsToScopeAsync(
                req.OrderId.Value,
                kioskScope.OrganizationId,
                kioskScope.StoreId,
                kioskScope.KioskId,
                cancellationToken);
            if (!isOrderValid)
            {
                return ApiResult<MaintenanceTicketResult>.Fail("The specified order does not belong to this kiosk scope.", 400);
            }
        }

        // 5. Validate optional DeviceEventId
        if (req.DeviceEventId.HasValue)
        {
            var isEventValid = await _ticketStore.DeviceEventBelongsToKioskAsync(req.DeviceEventId.Value, req.KioskId, cancellationToken);
            if (!isEventValid)
            {
                return ApiResult<MaintenanceTicketResult>.Fail("The specified device event does not belong to this kiosk.", 400);
            }
        }

        // 6. Generate Ticket Number
        var ticketNumber = await MaintenanceTicketNumberGenerator.GenerateAsync(_ticketStore, cancellationToken);

        // 7. Instantiate MaintenanceTicket
        var ticket = new MaintenanceTicket
        {
            Id = Guid.NewGuid(),
            OrganizationId = kioskScope.OrganizationId,
            StoreId = kioskScope.StoreId,
            KioskId = kioskScope.KioskId,
            DeviceId = req.DeviceId,
            OrderId = req.OrderId,
            DeviceEventId = req.DeviceEventId,
            TicketNumber = ticketNumber,
            IssueCode = string.IsNullOrWhiteSpace(req.IssueCode) ? "GENERAL" : req.IssueCode.Trim(),
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
            Priority = req.Priority ?? MaintenancePriority.Medium,
            Status = MaintenanceTicketStatus.Open,
            ReportedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = user.AccountId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _ticketStore.AddAsync(ticket, cancellationToken);
        await _ticketStore.SaveChangesAsync(cancellationToken);

        var result = MaintenanceTicketResultMapper.ToResult(ticket);

        await _publisher.PublishMaintenanceTicketChangedAsync(new MaintenanceTicketChangedEvent
        {
            TicketId = result.Id,
            TicketNumber = result.TicketNumber,
            KioskId = result.KioskId,
            OrganizationId = result.OrganizationId,
            StoreId = result.StoreId,
            OldStatus = null,
            NewStatus = result.Status.ToString(),
            Priority = result.Priority.ToString(),
            UpdatedAt = result.UpdatedAt ?? result.CreatedAt,
            Version = 1
        }, cancellationToken);

        return ApiResult<MaintenanceTicketResult>.Success(result, "Maintenance ticket created successfully.", 201);
    }
}
