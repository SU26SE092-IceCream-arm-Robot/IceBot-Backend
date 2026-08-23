using Application.ClientDevices.Abstractions;
using Application.ClientDevices.Security;
using Application.Identity.Abstractions;
using Domain.Devices.ClientDevices;
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
        var orderAccessKeyRingDirectory = configuration["ClientRuntime:OrderAccessKeyRingDirectory"];
        if (environment.IsProduction() && string.IsNullOrWhiteSpace(orderAccessKeyRingDirectory))
        {
            throw new InvalidOperationException(
                "ClientRuntime:OrderAccessKeyRingDirectory is required in Production to preserve client order access tokens across restarts and instances.");
        }

        var dataProtection = services.AddDataProtection().SetApplicationName("IceBot.WebAPI");
        if (!string.IsNullOrWhiteSpace(orderAccessKeyRingDirectory))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(orderAccessKeyRingDirectory));
        }

        services.AddSingleton<IOrderAccessTokenService, OrderAccessTokenService>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection("Authentication:Jwt"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Secret), "JWT Secret is required.")
            .ValidateOnStart();

        services.AddOptions<ClientDeviceSecurityOptions>()
            .Bind(configuration.GetSection(ClientDeviceSecurityOptions.SectionName))
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.CurrentHashKeyVersion) &&
                options.HashKeys.TryGetValue(options.CurrentHashKeyVersion, out var key) &&
                !string.IsNullOrWhiteSpace(key),
                "A current client-device credential hash key is required.")
            .Validate(options => options.HashKeys.Count != 0 && options.HashKeys.All(pair =>
                    !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value)),
                "Every configured client-device credential hash-key version must have a non-empty key.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.JwtSecret) && options.JwtSecret.Length >= 32,
                "A client-device JWT secret of at least 32 characters is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer) && !string.IsNullOrWhiteSpace(options.Audience),
                "A client-device JWT issuer and audience are required.")
            .Validate(options => options.TokenLifetimeMinutes is >= 1 and <= 60,
                "Client-device token lifetime must be between 1 and 60 minutes.")
            .Validate(options => options.LastSeenMinimumIntervalMinutes is >= 1 and <= 60,
                "Client-device last-seen interval must be between 1 and 60 minutes.")
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
        var clientDevice = configuration.GetSection(ClientDeviceSecurityOptions.SectionName).Get<ClientDeviceSecurityOptions>()!;
        if (string.Equals(jwt.Secret, clientDevice.JwtSecret, StringComparison.Ordinal) ||
            string.Equals(jwt.Issuer, clientDevice.Issuer, StringComparison.Ordinal) ||
            string.Equals(jwt.Audience, clientDevice.Audience, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Account and client-device JWT configurations must use distinct secret, issuer, and audience values.");
        }
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));
        var clientDeviceKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clientDevice.JwtSecret));
        services.AddSingleton<IClientDeviceTokenIssuer, ClientDeviceTokenIssuer>();

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
        })
        .AddJwtBearer(ClientDeviceAuthenticationDefaults.Scheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = clientDeviceKey,
                ValidateIssuer = true,
                ValidIssuer = clientDevice.Issuer,
                ValidateAudience = true,
                ValidAudience = clientDevice.Audience,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    var deviceIdValue = principal?.FindFirst(ClientDeviceAuthenticationDefaults.ClientDeviceIdClaim)?.Value;
                    var credentialVersionValue = principal?.FindFirst(ClientDeviceAuthenticationDefaults.CredentialVersionClaim)?.Value;
                    var sessionVersionValue = principal?.FindFirst(ClientDeviceAuthenticationDefaults.SessionVersionClaim)?.Value;
                    if (!Guid.TryParse(deviceIdValue, out var deviceId) ||
                        !int.TryParse(credentialVersionValue, out var credentialVersion) ||
                        !int.TryParse(sessionVersionValue, out var sessionVersion))
                    {
                        context.Fail("The client-device token is invalid.");
                        return;
                    }

                    var store = context.HttpContext.RequestServices.GetRequiredService<IClientDeviceStore>();
                    var device = await store.GetByIdAsync(deviceId, tracking: false, context.HttpContext.RequestAborted);
                    if (device is null || device.Type != ClientDeviceType.SelfOrderTablet ||
                        !device.MatchesAuthentication(credentialVersion, sessionVersion))
                    {
                        context.Fail("The client-device token is no longer authorized.");
                        return;
                    }

                    context.HttpContext.RequestServices.GetRequiredService<ICurrentClientDeviceContext>()
                        .Set(device.Id, device.OrganizationId, device.StoreId, device.KioskId);

                    try
                    {
                        var observationOptions = context.HttpContext.RequestServices
                            .GetRequiredService<Microsoft.Extensions.Options.IOptions<ClientDeviceSecurityOptions>>().Value;
                        await store.TryObserveAsync(
                            device.Id,
                            DateTimeOffset.UtcNow,
                            TimeSpan.FromMinutes(observationOptions.LastSeenMinimumIntervalMinutes),
                            context.HttpContext.RequestAborted);
                    }
                    catch
                    {
                        // Presence telemetry never changes runtime authentication or request correctness.
                    }
                }
            };
        });

        return services;
    }
}
