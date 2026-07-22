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

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {

    }

    app.UseIceBotSwagger();

    app.UseHttpsRedirection();

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
