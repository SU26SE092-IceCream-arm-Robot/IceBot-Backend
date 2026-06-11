using Application.Identity.Tokens.Claims;
using Domain.Orders.Enums;

namespace Application.Orders.Management.Queries;

public sealed class ListManagementOrdersQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public string? Search { get; init; }
    public OrderStatus? Status { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
