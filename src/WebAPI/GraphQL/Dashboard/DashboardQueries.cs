using Application.Dashboard.Abstractions;
using Application.Dashboard.Queries;
using HotChocolate.Authorization;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.GraphQL.Dashboard;

[ExtendObjectType("Query")]
public sealed class DashboardQueries
{
    [Authorize(Policy = "orders.view")]
    public async Task<DashboardMetrics> GetDashboard(
        ClaimsPrincipal claimsPrincipal,
        [Service] GetManagementDashboardQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var userContext = claimsPrincipal.GetUserContext();
        var query = new GetManagementDashboardQuery { UserContext = userContext };
        var result = await handler.HandleAsync(query, cancellationToken);

        if (!result.Succeeded)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(result.Message ?? "Failed to retrieve dashboard metrics.")
                    .SetCode(result.StatusCode.ToString())
                    .Build());
        }

        return result.Data!;
    }
}
