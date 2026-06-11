using Application.Tenants.TenantTree.Queries;
using Application.Tenants.TenantTree.Results;
using HotChocolate.Authorization;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.GraphQL.Tenants;

[ExtendObjectType("Query")]
public sealed class TenantQueries
{
    [Authorize(Policy = "tenant-tree.view")]
    public async Task<TenantTreeResult> GetTenantTree(
        bool includeInactive,
        ClaimsPrincipal claimsPrincipal,
        [Service] GetTenantTreeQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var userContext = claimsPrincipal.GetUserContext();
        var query = new GetTenantTreeQuery { UserContext = userContext, IncludeInactive = includeInactive };
        var result = await handler.HandleAsync(query, cancellationToken);

        if (!result.Succeeded)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(result.Message ?? "Failed to retrieve tenant tree.")
                    .SetCode(result.StatusCode.ToString())
                    .Build());
        }

        return result.Data!;
    }
}
