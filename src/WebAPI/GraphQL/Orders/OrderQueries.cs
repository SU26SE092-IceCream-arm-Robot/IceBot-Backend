using Application.Orders.Management.Queries;
using Application.Orders.Management.Results;
using Domain.Orders.Enums;
using HotChocolate.Authorization;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.GraphQL.Orders;

[ExtendObjectType("Query")]
public sealed class OrderQueries
{
    [Authorize(Policy = "orders.view")]
    public async Task<OrderOverviewResult> GetOrderOverview(
        DateTimeOffset? from,
        DateTimeOffset? to,
        OrderStatus? status,
        Guid? kioskId,
        int take,
        ClaimsPrincipal claimsPrincipal,
        [Service] GetOrderOverviewQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var userContext = claimsPrincipal.GetUserContext();
        var query = new GetOrderOverviewQuery
        {
            UserContext = userContext,
            From = from,
            To = to,
            Status = status,
            KioskId = kioskId,
            Take = take
        };
        var result = await handler.HandleAsync(query, cancellationToken);

        if (!result.Succeeded)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(result.Message ?? "Failed to retrieve order overview.")
                    .SetCode(result.StatusCode.ToString())
                    .Build());
        }

        return result.Data!;
    }
}
