using Application;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using WebAPI.Authorization;
using WebAPI.Configuration.Diagnostics;
using WebAPI.Configuration.Documentation;
using WebAPI.Configuration.Hosting;
using WebAPI.Configuration.Observability;
using WebAPI.Configuration.Security;
using WebAPI.GraphQL;
using WebAPI.Middlewares;
using WebAPI.SignalR;


Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .WriteTo.File("Logs/bootstrap-.txt", rollingInterval: RollingInterval.Day)
                        .CreateBootstrapLogger();

try
{
    Log.Information("Starting up the application...");

    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.UseIceBotExecutionEndpointMutualTls();

    ExecutionEndpointMutualTlsListenerConfigurationValidator.Validate(
        builder.Configuration,
        builder.Environment);

    builder.AddIceBotObservability();
    builder.Services.AddIceBotForwardedHeaders(builder.Configuration, builder.Environment);
    builder.Services.AddIceBotCors(builder.Configuration, builder.Environment);
    builder.Services.AddIceBotClientDeviceRuntime(builder.Configuration);
    builder.Services.AddIceBotAuthentication(builder.Configuration, builder.Environment);
    builder.Services.AddAuthorization(options => options.AddIceBotAuthorizationPolicies());
    builder.Services.AddIceBotRateLimiting();

    builder.Services.AddSingleton<IAuthorizationHandler, ScopedRoleAuthorizationHandler>();

    builder.Services.AddIceBotControllers();
    builder.Services.AddIceBotApiVersioning();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddIceBotSwagger();
    builder.Services.AddIceBotGraphQL();
    builder.Services.AddIceBotSignalR();

    var app = builder.Build();

    if (await app.TryRunIceBotMaintenanceCommandAsync(args)) return;

    app.UseIceBotSwagger();

    // The local Next.js proxy targets the Development HTTP listener. Keep the
    // production redirect, but avoid redirecting proxied local API calls to a
    // browser-facing self-signed HTTPS endpoint.
    if (!app.Environment.IsDevelopment())
    {
        app.UseForwardedHeaders();
        app.UseHttpsRedirection();
    }

    app.UseCors("FrontendOnly");

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseMiddleware<ExecutionRequestBodyHashMiddleware>();

    if (app.Configuration.GetValue<bool>("Observability:DebugBodyLogging:Enabled"))
    {
        app.UseMiddleware<DebugBodyLoggingMiddleware>();
    }

    app.UseAuthentication();

    app.UseRateLimiter();

    app.UseMiddleware<OrganizationAccessScopeMiddleware>();

    app.UseAuthorization();

    app.MapHealthEndpoints();
    app.MapApplicationInfoEndpoints();
    app.MapControllers();
    app.MapIceBotGraphQL();
    app.MapIceBotSignalR();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
