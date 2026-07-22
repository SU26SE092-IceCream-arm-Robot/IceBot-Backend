using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.RobotConfiguration.Storage.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.Infrastructure;

public sealed class PackageApiWebApplicationFactory(
    IntegrationTestFixture fixture,
    IArtifactObjectStorage objectStorage,
    Guid actorId,
    string role = "SystemAdmin",
    IReadOnlyCollection<string>? roleScopes = null) : WebApplicationFactory<Program>
{
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationHandler.AuthenticationSchemeName);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:IceBot_DB"] = fixture.ConnectionString,
                ["Email:InvitationBaseUrl"] = "http://localhost/accept-invitation",
                ["Email:PasswordResetBaseUrl"] = "http://localhost/reset-password",
                ["Firebase:Enabled"] = "false",
                ["Observability:OpenTelemetry:Enabled"] = "false"
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IArtifactObjectStorage>();
            services.AddSingleton(objectStorage);
            services.AddSingleton(new TestActorIdentity(actorId, role, roleScopes ?? []));
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationSchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationSchemeName;
                    options.DefaultScheme = TestAuthenticationHandler.AuthenticationSchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationSchemeName,
                    _ => { });
        });
    }
}

internal sealed record TestActorIdentity(
    Guid AccountId,
    string Role,
    IReadOnlyCollection<string> RoleScopes);

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TestActorIdentity actor)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationSchemeName = "IntegrationTest";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, actor.AccountId.ToString("D")),
            new Claim(ClaimTypes.Name, $"Package integration {actor.Role}"),
            new Claim(ClaimTypes.Role, actor.Role)
        };
        claims.AddRange(actor.RoleScopes.Select(scope => new Claim("role_scope", scope)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationSchemeName));
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, AuthenticationSchemeName)));
    }
}
