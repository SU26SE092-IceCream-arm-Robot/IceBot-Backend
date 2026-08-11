using Application;
using Infrastructure;
using Infrastructure.Catalog.Bootstrap;
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

    builder.Services.AddSingleton<IAuthorizationHandler, ScopedRoleAuthorizationHandler>();

    builder.Services.AddIceBotControllers();
    builder.Services.AddIceBotApiVersioning();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddIceBotSwagger();
    builder.Services.AddIceBotGraphQL();
    builder.Services.AddIceBotSignalR();

    var app = builder.Build();

    if (args.Contains("--reset-robot-authoring-automation-test", StringComparer.OrdinalIgnoreCase))
    {
        if (!app.Environment.IsDevelopment())
            throw new InvalidOperationException("Robot authoring automation reset is available only in Development.");

        await using var scope = app.Services.CreateAsyncScope();
        var reset = scope.ServiceProvider.GetRequiredService<DevelopmentRobotAuthoringAutomationReset>();
        var result = await reset.ResetAsync(CancellationToken.None);
        Log.Information(
            "Reset {OrganizationCode} ({OrganizationId}): {Imports} imports, {Artifacts} artifacts, {Programs} programs, {Contracts} contracts, {Bindings} bindings, {Releases} releases, {MenuItems} menu items, {Objects} objects deleted, {RetainedObjects} objects retained.",
            DevelopmentRobotAuthoringAutomationReset.OrganizationCode, result.OrganizationId,
            result.DeletedImportCount, result.DeletedArtifactCount, result.DeletedProgramCount,
            result.DeletedContractCount, result.DeletedBindingCount, result.DeletedReleaseCount,
            result.DeletedMenuItemCount, result.DeletedObjectCount, result.RetainedObjectCount);
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

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseMiddleware<ExecutionRequestBodyHashMiddleware>();

    if (app.Configuration.GetValue<bool>("Observability:DebugBodyLogging:Enabled"))
    {
        app.UseMiddleware<DebugBodyLoggingMiddleware>();
    }

    app.UseAuthentication();

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
