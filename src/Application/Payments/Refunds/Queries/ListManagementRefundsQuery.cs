using Application.Identity.Tokens.Claims;
using Domain.Payments.Enums;

namespace Application.Payments.Refunds.Queries;

public sealed class ListManagementRefundsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public string? Search { get; init; }
    public RefundStatus? Status { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
