using Application;
using Infrastructure;
using Infrastructure.Catalog.Bootstrap;
using Microsoft.AspNetCore.Authorization;
using System.Threading.RateLimiting;
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

    builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                                    .AddEnvironmentVariables();

    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration.AddUserSecrets<Program>(optional: true);
    }

    builder.AddIceBotObservability();

    builder.Services.AddIceBotCors(builder.Configuration, builder.Environment);
    builder.Services.AddIceBotAuthentication(builder.Configuration, builder.Environment);
    builder.Services.AddAuthorization(options => options.AddIceBotAuthorizationPolicies());
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("service-registration-submission", context =>
        {
            var address = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(address, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            });
        });
    });

    builder.Services.AddSingleton<IAuthorizationHandler, ScopedRoleAuthorizationHandler>();

    builder.Services.AddIceBotControllers();
    builder.Services.AddIceBotApiVersioning();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddIceBotSwagger();
    builder.Services.AddIceBotGraphQL();
    builder.Services.AddIceBotSignalR();

    var app = builder.Build();

    if (args.Contains("--delete-legacy-automation-fixture", StringComparer.OrdinalIgnoreCase))
    {
        if (!app.Environment.IsDevelopment())
            throw new InvalidOperationException("Legacy automation fixture deletion is available only in Development.");

        await using var scope = app.Services.CreateAsyncScope();
        var reset = scope.ServiceProvider.GetRequiredService<DevelopmentIceBotDemoReset>();
        var deleted = await reset.DeleteLegacyAutomationFixtureAsync(CancellationToken.None);
        Log.Information("Deleted legacy ICEBOT-AUTOMATION-TEST fixture organization: {Deleted}.", deleted);
        return;
    }

    if (args.Contains("--reset-icebot-demo", StringComparer.OrdinalIgnoreCase))
    {
        if (!app.Environment.IsDevelopment())
            throw new InvalidOperationException("ICEBOT-DEMO reset is available only in Development.");

        await using var scope = app.Services.CreateAsyncScope();
        var reset = scope.ServiceProvider.GetRequiredService<DevelopmentIceBotDemoReset>();
        var result = await reset.ResetAsync(CancellationToken.None);
        Log.Information(
            "Reset {OrganizationCode} ({OrganizationId}): {Imports} imports, {Artifacts} artifacts, {Programs} programs, {Contracts} contracts, {Bindings} bindings, {Releases} releases, {MenuItems} menu items, {Objects} objects deleted, {RetainedObjects} objects retained. Deleted legacy automation fixture: {DeletedAutomationFixture}.",
            DevelopmentIceBotDemoReset.OrganizationCode, result.OrganizationId,
            result.DeletedImportCount, result.DeletedArtifactCount, result.DeletedProgramCount,
            result.DeletedContractCount, result.DeletedBindingCount, result.DeletedReleaseCount,
            result.DeletedMenuItemCount, result.DeletedObjectCount, result.RetainedObjectCount,
            result.DeletedAutomationFixture);
        return;
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {

    }

    app.UseIceBotSwagger();

    // The local Next.js proxy targets the Development HTTP listener. Keep the
    // production redirect, but avoid redirecting proxied local API calls to a
    // browser-facing self-signed HTTPS endpoint.
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseCors("FrontendOnly");

    app.UseRateLimiter();

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseMiddleware<ExecutionRequestBodyHashMiddleware>();

    if (app.Configuration.GetValue<bool>("Observability:DebugBodyLogging:Enabled"))
    {
        app.UseMiddleware<DebugBodyLoggingMiddleware>();
    }

    app.UseAuthentication();

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
