using Application.Identity.Tokens.Claims;

namespace Application.Payments.Refunds.Queries;

public sealed class GetManagementRefundQuery
{
    public required Guid RefundId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}
