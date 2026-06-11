using Application.Inventory.Queries;
using Application.Inventory.Results;
using HotChocolate.Authorization;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.GraphQL.Inventory;

[ExtendObjectType("Query")]
public sealed class InventoryQueries
{
    [Authorize(Policy = "inventory.view")]
    public async Task<InventorySummaryResult> GetInventorySummary(
        Guid? kioskId,
        Guid? storeId,
        ClaimsPrincipal claimsPrincipal,
        [Service] GetInventorySummaryQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var userContext = claimsPrincipal.GetUserContext();
        var query = new GetInventorySummaryQuery
        {
            UserContext = userContext,
            KioskId = kioskId,
            StoreId = storeId
        };
        var result = await handler.HandleAsync(query, cancellationToken);

        if (!result.Succeeded)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(result.Message ?? "Failed to retrieve inventory summary.")
                    .SetCode(result.StatusCode.ToString())
                    .Build());
        }

        return result.Data!;
    }
}
