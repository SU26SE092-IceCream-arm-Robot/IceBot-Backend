using Application.Identity.Tokens.Claims;
using System;

namespace Application.Orders.Management.Queries;

public sealed class GetManagementOrderQuery
{
    public required Guid OrderId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}
