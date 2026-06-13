using Application.Operations.MaintenanceTickets.Commands;
using Application.Operations.MaintenanceTickets.Queries;
using Application.Operations.MaintenanceTickets.Requests;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authorization;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebAPI.Controllers.Operations;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/management")]
public class ManagementMaintenanceTicketsController : ControllerBase
{
    private readonly CreateMaintenanceTicketCommandHandler _createHandler;
    private readonly UpdateMaintenanceTicketCommandHandler _updateHandler;
    private readonly AssignMaintenanceTicketCommandHandler _assignHandler;
    private readonly StartMaintenanceTicketCommandHandler _startHandler;
    private readonly ResolveMaintenanceTicketCommandHandler _resolveHandler;
    private readonly CloseMaintenanceTicketCommandHandler _closeHandler;
    private readonly CancelMaintenanceTicketCommandHandler _cancelHandler;
    private readonly GetMaintenanceTicketQueryHandler _getHandler;
    private readonly ListMaintenanceTicketsQueryHandler _listHandler;

    public ManagementMaintenanceTicketsController(
        CreateMaintenanceTicketCommandHandler createHandler,
        UpdateMaintenanceTicketCommandHandler updateHandler,
        AssignMaintenanceTicketCommandHandler assignHandler,
        StartMaintenanceTicketCommandHandler startHandler,
        ResolveMaintenanceTicketCommandHandler resolveHandler,
        CloseMaintenanceTicketCommandHandler closeHandler,
        CancelMaintenanceTicketCommandHandler cancelHandler,
        GetMaintenanceTicketQueryHandler getHandler,
        ListMaintenanceTicketsQueryHandler listHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _assignHandler = assignHandler;
        _startHandler = startHandler;
        _resolveHandler = resolveHandler;
        _closeHandler = closeHandler;
        _cancelHandler = cancelHandler;
        _getHandler = getHandler;
        _listHandler = listHandler;
    }

    [HttpGet("maintenance-tickets")]
    [Authorize(Policy = "maintenance.view")]
    public async Task<IActionResult> ListMaintenanceTickets(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? kioskId,
        [FromQuery] Guid? assignedToAccountId,
        [FromQuery] Guid? createdByAccountId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var context = User.GetUserContext();
        var query = new ListMaintenanceTicketsQuery
        {
            UserContext = context,
            Status = status,
            Priority = priority,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            AssignedToAccountId = assignedToAccountId,
            CreatedByAccountId = createdByAccountId,
            FromDate = fromDate,
            ToDate = toDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _listHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("maintenance-tickets/{id:guid}")]
    [Authorize(Policy = "maintenance.view")]
    public async Task<IActionResult> GetMaintenanceTicket(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var context = User.GetUserContext();
        var query = new GetMaintenanceTicketQuery
        {
            TicketId = id,
            UserContext = context
        };

        var result = await _getHandler.HandleAsync(query, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("maintenance-tickets")]
    [Authorize(Policy = "maintenance.create")]
    public async Task<IActionResult> CreateMaintenanceTicket(
        [FromBody] CreateMaintenanceTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = User.GetUserContext();
        var command = new CreateMaintenanceTicketCommand
        {
            UserContext = context,
            Request = request
        };

        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("maintenance-tickets/{id:guid}")]
    [Authorize(Policy = "maintenance.manage")]
    public async Task<IActionResult> UpdateMaintenanceTicket(
        Guid id,
        [FromBody] UpdateMaintenanceTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = User.GetUserContext();
        var command = new UpdateMaintenanceTicketCommand
        {
            TicketId = id,
            UserContext = context,
            Request = request
        };

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("maintenance-tickets/{id:guid}/assign")]
    [Authorize(Policy = "maintenance.manage")]
    public async Task<IActionResult> AssignMaintenanceTicket(
        Guid id,
        [FromBody] AssignMaintenanceTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = User.GetUserContext();
        var command = new AssignMaintenanceTicketCommand
        {
            TicketId = id,
            UserContext = context,
            Request = request
        };

        var result = await _assignHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("maintenance-tickets/{id:guid}/start")]
    [Authorize(Policy = "maintenance.manage")]
    public async Task<IActionResult> StartMaintenanceTicket(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var context = User.GetUserContext();
        var command = new StartMaintenanceTicketCommand
        {
            TicketId = id,
            UserContext = context
        };

        var result = await _startHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("maintenance-tickets/{id:guid}/resolve")]
    [Authorize(Policy = "maintenance.manage")]
    public async Task<IActionResult> ResolveMaintenanceTicket(
        Guid id,
        [FromBody] ResolveMaintenanceTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = User.GetUserContext();
        var command = new ResolveMaintenanceTicketCommand
        {
            TicketId = id,
            UserContext = context,
            Request = request
        };

        var result = await _resolveHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("maintenance-tickets/{id:guid}/close")]
    [Authorize(Policy = "maintenance.manage")]
    public async Task<IActionResult> CloseMaintenanceTicket(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var context = User.GetUserContext();
        var command = new CloseMaintenanceTicketCommand
        {
            TicketId = id,
            UserContext = context
        };

        var result = await _closeHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("maintenance-tickets/{id:guid}/cancel")]
    [Authorize(Policy = "maintenance.manage")]
    public async Task<IActionResult> CancelMaintenanceTicket(
        Guid id,
        [FromBody] CancelMaintenanceTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = User.GetUserContext();
        var command = new CancelMaintenanceTicketCommand
        {
            TicketId = id,
            UserContext = context,
            Request = request
        };

        var result = await _cancelHandler.HandleAsync(command, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
