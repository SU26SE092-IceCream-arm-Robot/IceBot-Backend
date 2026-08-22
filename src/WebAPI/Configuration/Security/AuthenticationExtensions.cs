using Application.Identity.Abstractions;
using Domain.Identity.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace WebAPI.Configuration.Security;

public static class AuthenticationExtensions
{
    public static IWebHostBuilder UseIceBotExecutionEndpointMutualTls(this IWebHostBuilder webHostBuilder)
    {
        webHostBuilder.ConfigureKestrel(options =>
        {
            options.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                httpsOptions.AllowAnyClientCertificate();
            });
        });
        return webHostBuilder;
    }

    public static IServiceCollection AddIceBotAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var publicOrderAccessKeyRingDirectory = configuration["PublicOrderAccess:KeyRingDirectory"];
        if (environment.IsProduction() && string.IsNullOrWhiteSpace(publicOrderAccessKeyRingDirectory))
        {
            throw new InvalidOperationException(
                "PublicOrderAccess:KeyRingDirectory is required in Production to preserve order access tokens across restarts and instances.");
        }

        var dataProtection = services.AddDataProtection().SetApplicationName("IceBot.WebAPI");
        if (!string.IsNullOrWhiteSpace(publicOrderAccessKeyRingDirectory))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(publicOrderAccessKeyRingDirectory));
        }

        services.AddSingleton<IPublicOrderAccessTokenService, PublicOrderAccessTokenService>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Authentication:Jwt"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Secret), "JWT Secret is required.")
            .ValidateOnStart();

        services.AddOptions<ExecutionEndpointSecurityOptions>()
            .Bind(configuration.GetSection(ExecutionEndpointSecurityOptions.SectionName))
            .Validate(options => options.SignedRequestMaxClockSkewSeconds is >= 30 and <= 900,
                "Signed request clock skew must be between 30 and 900 seconds.")
            .Validate(options => options.NonceRetentionSeconds is >= 300 and <= 86400,
                "Execution request nonce retention must be between 300 and 86400 seconds.")
            .Validate(options => options.NonceRetentionSeconds >= options.SignedRequestMaxClockSkewSeconds * 2,
                "Execution request nonce retention must cover twice the signed-request clock-skew window.")
            .Validate(options => options.MaxRequestBodyBytes is >= 1024 and <= 10_485_760,
                "Execution request body limit must be between 1 KiB and 10 MiB.")
            .ValidateOnStart();
        services.AddScoped<ExecutionEndpointRequestAuthenticator>();

        var jwt = configuration.GetSection("Authentication:Jwt").Get<JwtOptions>()!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = !string.IsNullOrWhiteSpace(jwt.Issuer),
                ValidateAudience = !string.IsNullOrWhiteSpace(jwt.Audience),
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var accountIdValue = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var versionValue = context.Principal?.FindFirst("authorization_version")?.Value;
                    if (!Guid.TryParse(accountIdValue, out var accountId) ||
                        !long.TryParse(versionValue, out var tokenVersion))
                    {
                        context.Fail("The access token does not contain a valid authorization version.");
                        return;
                    }

                    var accounts = context.HttpContext.RequestServices.GetRequiredService<IIdentityAccountStore>();
                    var account = await accounts.GetByIdAsync(accountId, asNoTracking: true, context.HttpContext.RequestAborted);
                    if (account is null || account.Status != AccountStatus.Active || account.AuthorizationVersion != tokenVersion)
                    {
                        context.Fail("The access token is no longer authorized.");
                    }
                }
            };
        });

        return services;
    }
}
