using Infrastructure.Data;

namespace WebAPI.Configuration;

public static class HealthEndpointExtensions
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new
        {
            status = "Healthy",
            checkedAt = DateTimeOffset.UtcNow
        })).AllowAnonymous();

        endpoints.MapGet("/health/ready", async (IceBotDbContext dbContext, CancellationToken cancellationToken) =>
        {
            try
            {
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                if (!canConnect)
                {
                    return Results.Json(
                        new
                        {
                            status = "Unhealthy",
                            database = "Unavailable",
                            checkedAt = DateTimeOffset.UtcNow
                        },
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                return Results.Ok(new
                {
                    status = "Healthy",
                    database = "Available",
                    checkedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Results.Json(
                    new
                    {
                        status = "Unhealthy",
                        database = "Unavailable",
                        error = ex.GetType().Name,
                        checkedAt = DateTimeOffset.UtcNow
                    },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).AllowAnonymous();

        return endpoints;
    }
}
