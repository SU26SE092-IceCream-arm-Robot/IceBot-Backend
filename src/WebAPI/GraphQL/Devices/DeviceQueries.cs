using Application.Devices.Catalog.Queries;
using Application.Devices.ExecutionEndpoints.Queries;
using Application.Devices.Telemetry.Queries;
using Application.Devices.Connectivity.Queries;
using Application.Devices.Catalog.Results;
using Application.Devices.ExecutionEndpoints.Results;
using Application.Devices.Telemetry.Results;
using Application.Devices.Connectivity.Results;
using Application.Devices.Credentials.Results;
using HotChocolate.Authorization;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.GraphQL.Devices;

[ExtendObjectType("Query")]
public sealed class DeviceQueries
{
    [Authorize(Policy = "operations.view")]
    public async Task<KioskStatusOverviewResult> GetKioskStatusOverview(
        Guid? organizationId,
        Guid? storeId,
        ClaimsPrincipal claimsPrincipal,
        [Service] GetKioskStatusOverviewQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var userContext = claimsPrincipal.GetUserContext();
        var query = new GetKioskStatusOverviewQuery
        {
            UserContext = userContext,
            OrganizationId = organizationId,
            StoreId = storeId
        };
        var result = await handler.HandleAsync(query, cancellationToken);

        if (!result.Succeeded)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(result.Message ?? "Failed to retrieve kiosk status overview.")
                    .SetCode(result.StatusCode.ToString())
                    .Build());
        }

        return result.Data!;
    }
}
