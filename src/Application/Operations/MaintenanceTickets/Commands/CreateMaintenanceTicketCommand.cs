using Application.Identity.Tokens.Claims;
using Application.Operations.Abstractions;
using Application.Operations.MaintenanceTickets.Mapping;
using Application.Operations.MaintenanceTickets.Requests;
using Application.Operations.MaintenanceTickets.Results;
using Application.Operations.MaintenanceTickets.Rules;
using Application.Shared.Wrappers;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Operations.MaintenanceTickets.Commands;

public sealed class CreateMaintenanceTicketCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required CreateMaintenanceTicketRequest Request { get; init; }
}

public sealed class CreateMaintenanceTicketCommandHandler
{
    private readonly IMaintenanceTicketStore _ticketStore;

    public CreateMaintenanceTicketCommandHandler(IMaintenanceTicketStore ticketStore)
    {
        _ticketStore = ticketStore;
    }

    public async Task<ApiResult<MaintenanceTicketResult>> HandleAsync(
        CreateMaintenanceTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = command.UserContext;
        var req = command.Request;

        // 1. Authorization Access Check
        if (!MaintenanceTicketAccessRules.CanCreate(user, req.OrganizationId, req.StoreId, req.KioskId))
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Access denied.", 403);
        }

        // 2. Validate Kiosk belongs to the Organization & Store
        var isValidKiosk = await _ticketStore.ValidateKioskScopeAsync(req.OrganizationId, req.StoreId, req.KioskId, cancellationToken);
        if (!isValidKiosk)
        {
            return ApiResult<MaintenanceTicketResult>.Fail("Kiosk does not exist within the specified organization and store.", 400);
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
            var isOrderValid = await _ticketStore.OrderBelongsToScopeAsync(req.OrderId.Value, req.OrganizationId, req.StoreId, req.KioskId, cancellationToken);
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
            OrganizationId = req.OrganizationId,
            StoreId = req.StoreId,
            KioskId = req.KioskId,
            DeviceId = req.DeviceId,
            OrderId = req.OrderId,
            DeviceEventId = req.DeviceEventId,
            TicketNumber = ticketNumber,
            IssueCode = req.IssueCode ?? "GENERAL",
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
        return ApiResult<MaintenanceTicketResult>.Success(result, "Maintenance ticket created successfully.", 201);
    }
}
