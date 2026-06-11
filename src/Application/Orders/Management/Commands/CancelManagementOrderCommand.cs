using Application.Identity.Tokens.Claims;
using System;

namespace Application.Orders.Management.Commands;

public sealed class CancelManagementOrderCommand
{
    public required Guid OrderId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public string? Reason { get; init; }
}
