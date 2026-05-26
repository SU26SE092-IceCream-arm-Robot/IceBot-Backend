using System.Reflection;

namespace WebAPI.Configuration;

public static class ApplicationInfoEndpointExtensions
{
    public static IEndpointRouteBuilder MapApplicationInfoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/info", (IHostEnvironment environment, IConfiguration configuration) =>
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString();
            var informationalVersion = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            return Results.Ok(new
            {
                service = "IceBot WebAPI",
                environment = environment.EnvironmentName,
                version = informationalVersion ?? version,
                commit = configuration["BUILD_COMMIT"] ?? configuration["Build:Commit"],
                builtAt = configuration["BUILD_TIME"] ?? configuration["Build:Time"],
                checkedAt = DateTimeOffset.UtcNow
            });
        }).AllowAnonymous();

        return endpoints;
    }
}
