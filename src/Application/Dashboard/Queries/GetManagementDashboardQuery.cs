using Application.Identity.Tokens.Claims;

namespace Application.Dashboard.Queries;

public sealed class GetManagementDashboardQuery
{
    public required CurrentUserContext UserContext { get; init; }
}
