using WebAPI.GraphQL.Dashboard;
using WebAPI.GraphQL.Devices;
using WebAPI.GraphQL.Inventory;
using WebAPI.GraphQL.Orders;
using WebAPI.GraphQL.Tenants;

namespace WebAPI.GraphQL;

public static class GraphQLEndpointExtensions
{
    public static IServiceCollection AddIceBotGraphQL(this IServiceCollection services)
    {
        services.AddGraphQLServer()
            .AddQueryType()
            .AddTypeExtension<DashboardQueries>()
            .AddTypeExtension<TenantQueries>()
            .AddTypeExtension<OrderQueries>()
            .AddTypeExtension<DeviceQueries>()
            .AddTypeExtension<InventoryQueries>()
            .AddAuthorization();

        return services;
    }

    public static IEndpointRouteBuilder MapIceBotGraphQL(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGraphQL().RequireAuthorization();
        return endpoints;
    }
}
