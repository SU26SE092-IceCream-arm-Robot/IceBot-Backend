using FirebaseAdmin.Auth;
using Application.Catalog.Images;
using Application.RobotConfiguration.Storage.Abstractions;
using Infrastructure.Data;
using Infrastructure.Firebase;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace WebAPI.Configuration.Diagnostics;

public static class HealthEndpointExtensions
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new
        {
            status = "Healthy"
        })).AllowAnonymous();

        endpoints.MapGet("/health/ready", async (IceBotDbContext dbContext, CancellationToken cancellationToken) =>
        {
            var overallStopwatch = Stopwatch.StartNew();
            var checks = new List<HealthCheckResult>();
            var isHealthy = true;

            var dbStopwatch = Stopwatch.StartNew();
            try
            {
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                dbStopwatch.Stop();

                if (canConnect)
                {
                    checks.Add(new HealthCheckResult("postgresql", "Healthy", dbStopwatch.ElapsedMilliseconds));
                }
                else
                {
                    isHealthy = false;
                    checks.Add(new HealthCheckResult("postgresql", "Unhealthy", dbStopwatch.ElapsedMilliseconds, "Database unavailable"));
                }
            }
            catch (Exception)
            {
                dbStopwatch.Stop();
                isHealthy = false;
                checks.Add(new HealthCheckResult("postgresql", "Unhealthy", dbStopwatch.ElapsedMilliseconds, "Database unavailable"));
            }

            overallStopwatch.Stop();

            var response = new HealthResponse(
                Status: isHealthy ? "Healthy" : "Unhealthy",
                Checks: checks,
                DurationMs: overallStopwatch.ElapsedMilliseconds,
                CheckedAt: DateTimeOffset.UtcNow
            );

            if (!isHealthy)
            {
                return Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Ok(response);
        }).AllowAnonymous();

        endpoints.MapGet("/management/diagnostics/health", async (
            HttpContext httpContext,
            IceBotDbContext dbContext,
            IConfiguration configuration,
            IFirebaseClient firebaseClient,
            ICatalogImageStorageHealthProbe catalogImageStorageHealthProbe,
            IArtifactObjectStorage artifactObjectStorage,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            if (!IsDiagnosticsRequestAllowed(httpContext, configuration, environment))
            {
                return Results.NotFound();
            }

            var overallStopwatch = Stopwatch.StartNew();
            var checks = new List<HealthCheckResult>();

            var dbStopwatch = Stopwatch.StartNew();
            var canConnect = false;
            try
            {
                canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                checks.Add(new HealthCheckResult(
                    "postgresql",
                    canConnect ? "Healthy" : "Unhealthy",
                    dbStopwatch.ElapsedMilliseconds,
                    canConnect ? null : "Database unavailable"));
            }
            catch (Exception)
            {
                checks.Add(new HealthCheckResult(
                    "postgresql",
                    "Unhealthy",
                    dbStopwatch.ElapsedMilliseconds,
                    "Database unavailable"));
            }
            finally
            {
                dbStopwatch.Stop();
            }

            var migrationStopwatch = Stopwatch.StartNew();
            if (canConnect)
            {
                try
                {
                    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
                    var pendingCount = pendingMigrations.Count();
                    checks.Add(new HealthCheckResult(
                        "migration",
                        pendingCount == 0 ? "Healthy" : "Degraded",
                        migrationStopwatch.ElapsedMilliseconds,
                        pendingCount == 0 ? null : $"Pending migrations detected: {pendingCount}"));
                }
                catch (Exception)
                {
                    checks.Add(new HealthCheckResult(
                        "migration",
                        "Degraded",
                        migrationStopwatch.ElapsedMilliseconds,
                        "Migration status unavailable"));
                }
                finally
                {
                    migrationStopwatch.Stop();
                }
            }
            else
            {
                migrationStopwatch.Stop();
                checks.Add(new HealthCheckResult(
                    "migration",
                    "Unhealthy",
                    migrationStopwatch.ElapsedMilliseconds,
                    "Skipped because database is unavailable"));
            }

            checks.Add(CheckJwtConfig(configuration));
            checks.Add(CheckSmtpConfig(configuration));
            checks.Add(CheckFirebaseConfig(configuration, environment));
            checks.Add(CheckPayOsConfig(configuration));
            checks.Add(CheckCloudinaryConfig(configuration));
            checks.Add(CheckArtifactObjectStorageConfig(configuration));
            checks.Add(CheckClientRuntimeKeyRing(configuration));
            checks.Add(CheckClientDeviceSecurityConfig(configuration));

            if (configuration.GetValue<bool>("Diagnostics:EnableExternalPing"))
            {
                var timeoutSeconds = Math.Clamp(
                    configuration.GetValue<int?>("Diagnostics:ExternalPingTimeoutSeconds") ?? 5,
                    1,
                    30);

                checks.Add(await CheckSmtpRealtimeAsync(configuration, timeoutSeconds, cancellationToken));
                checks.Add(await CheckFirebaseRealtimeAsync(configuration, firebaseClient, timeoutSeconds, cancellationToken));
                checks.Add(await CheckPayOsRealtimeAsync(configuration, httpClientFactory, timeoutSeconds, cancellationToken));
                checks.Add(await CheckCloudinaryRealtimeAsync(catalogImageStorageHealthProbe, timeoutSeconds, cancellationToken));
                checks.Add(await CheckArtifactObjectStorageRealtimeAsync(artifactObjectStorage, timeoutSeconds, cancellationToken));
            }
            else
            {
                checks.Add(new HealthCheckResult("external_ping", "Skipped", 0,
                    "Realtime SMTP/Firebase/PayOS/Cloudinary/object-storage ping is disabled."));
            }

            overallStopwatch.Stop();

            var status = DetermineOverallStatus(checks);
            var response = new HealthResponse(
                Status: status,
                Checks: checks,
                DurationMs: overallStopwatch.ElapsedMilliseconds,
                CheckedAt: DateTimeOffset.UtcNow);

            var statusCode = status == "Unhealthy"
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status200OK;

            return Results.Json(response, statusCode: statusCode);
        }).AllowAnonymous();

        return endpoints;
    }

    private static bool IsDiagnosticsRequestAllowed(
        HttpContext httpContext,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var configuredKey = configuration["Diagnostics:ApiKey"];

        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return environment.IsDevelopment();
        }

        return httpContext.Request.Headers.TryGetValue("X-Diagnostics-Key", out var providedKey) &&
               string.Equals(providedKey.ToString(), configuredKey, StringComparison.Ordinal);
    }

    private static HealthCheckResult CheckJwtConfig(IConfiguration configuration)
    {
        var secret = configuration["Authentication:Jwt:Secret"];
        var issuer = configuration["Authentication:Jwt:Issuer"];
        var audience = configuration["Authentication:Jwt:Audience"];

        if (IsMissingOrPlaceholder(secret))
        {
            return new HealthCheckResult("jwt_config", "Missing", 0, "JWT secret is missing or placeholder.");
        }

        if (secret!.Length < 32)
        {
            return new HealthCheckResult("jwt_config", "Degraded", 0, "JWT secret is shorter than recommended.");
        }

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            return new HealthCheckResult("jwt_config", "Degraded", 0, "JWT issuer or audience is missing.");
        }

        return new HealthCheckResult("jwt_config", "Healthy", 0);
    }

    private static HealthCheckResult CheckSmtpConfig(IConfiguration configuration)
    {
        var host = configuration["Email:Host"];
        var from = configuration["Email:From"];
        var userName = configuration["Email:UserName"];
        var password = configuration["Email:Password"];

        if (IsMissingOrPlaceholder(host) ||
            IsMissingOrPlaceholder(from) ||
            IsMissingOrPlaceholder(userName) ||
            IsMissingOrPlaceholder(password))
        {
            return new HealthCheckResult("smtp_config", "Missing", 0, "SMTP config is missing or placeholder.");
        }

        return new HealthCheckResult("smtp_config", "Healthy", 0);
    }

    private static async Task<HealthCheckResult> CheckSmtpRealtimeAsync(
        IConfiguration configuration,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var host = configuration["Email:Host"];
        var port = configuration.GetValue<int?>("Email:Port") ?? 587;
        var userName = configuration["Email:UserName"];
        var password = configuration["Email:Password"];

        if (IsMissingOrPlaceholder(host) ||
            IsMissingOrPlaceholder(userName) ||
            IsMissingOrPlaceholder(password))
        {
            return new HealthCheckResult("smtp_realtime", "Skipped", stopwatch.ElapsedMilliseconds, "SMTP config is incomplete.");
        }

        return await CheckExternalProbeAsync(
            "smtp_realtime",
            "SMTP realtime ping failed.",
            timeoutSeconds,
            cancellationToken,
            async timeoutToken =>
            {
            using var client = new SmtpClient();
            var secureSocketOptions = ResolveSmtpSecureSocketOptions(configuration, port);
            var smtpHost = host!;
            var smtpUserName = userName!;
            var smtpPassword = password!;

            await client.ConnectAsync(smtpHost, port, secureSocketOptions, timeoutToken);
            await client.AuthenticateAsync(smtpUserName, smtpPassword, timeoutToken);
            await client.DisconnectAsync(true, timeoutToken);
            });
    }

    private static HealthCheckResult CheckFirebaseConfig(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var enabled = configuration.GetValue<bool?>("Firebase:Enabled") ?? true;
        if (!enabled)
        {
            return new HealthCheckResult("firebase_config", "Disabled", 0, "Firebase integration is disabled.");
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS")))
        {
            return new HealthCheckResult("firebase_config", "Healthy", 0);
        }

        try
        {
            var credentialsPath = FirebaseCredentialFileResolver.Resolve(configuration, environment);
            if (credentialsPath is not null && File.Exists(credentialsPath))
            {
                return new HealthCheckResult("firebase_config", "Healthy", 0);
            }
        }
        catch (InvalidOperationException ex)
        {
            return new HealthCheckResult("firebase_config", "Invalid", 0, ex.Message);
        }

        return new HealthCheckResult("firebase_config", "Missing", 0, "Firebase credentials are not configured.");
    }

    private static async Task<HealthCheckResult> CheckFirebaseRealtimeAsync(
        IConfiguration configuration,
        IFirebaseClient firebaseClient,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var enabled = configuration.GetValue<bool?>("Firebase:Enabled") ?? true;
        if (!enabled)
        {
            return new HealthCheckResult("firebase_realtime", "Skipped", stopwatch.ElapsedMilliseconds, "Firebase integration is disabled.");
        }

        return await CheckExternalProbeAsync(
            "firebase_realtime",
            "Firebase realtime ping failed.",
            timeoutSeconds,
            cancellationToken,
            async timeoutToken =>
            {
                try
                {
                    await firebaseClient.GetUserAsync("__icebot_diagnostics_probe__", timeoutToken);
                }
                catch (FirebaseAuthException ex) when (IsExpectedFirebaseProbeMiss(ex))
                {
                    // A missing synthetic user proves authenticated reachability.
                }
            });
    }

    private static HealthCheckResult CheckPayOsConfig(IConfiguration configuration)
    {
        var clientId = configuration["PayOS:ClientId"];
        var apiKey = configuration["PayOS:ApiKey"];
        var checksumKey = configuration["PayOS:ChecksumKey"];
        var baseUrl = configuration["PayOS:BaseUrl"];
        var returnUrl = configuration["PayOS:ReturnUrl"];
        var cancelUrl = configuration["PayOS:CancelUrl"];

        if (IsMissingOrPlaceholder(clientId) ||
            IsMissingOrPlaceholder(apiKey) ||
            IsMissingOrPlaceholder(checksumKey) ||
            IsMissingOrPlaceholder(baseUrl) ||
            string.IsNullOrWhiteSpace(returnUrl) ||
            string.IsNullOrWhiteSpace(cancelUrl))
        {
            return new HealthCheckResult("payos_config", "Missing", 0, "PayOS config is missing or placeholder.");
        }

        return new HealthCheckResult("payos_config", "Healthy", 0);
    }

    private static HealthCheckResult CheckCloudinaryConfig(IConfiguration configuration)
    {
        var cloudName = configuration["Media:Cloudinary:CloudName"];
        var apiKey = configuration["Media:Cloudinary:ApiKey"];
        var apiSecret = configuration["Media:Cloudinary:ApiSecret"];
        var rootFolder = configuration["Media:Cloudinary:RootFolder"];

        return IsMissingOrPlaceholder(cloudName) ||
               IsMissingOrPlaceholder(apiKey) ||
               IsMissingOrPlaceholder(apiSecret) ||
               string.IsNullOrWhiteSpace(rootFolder)
            ? new HealthCheckResult("cloudinary_config", "Missing", 0, "Cloudinary config is missing or placeholder.")
            : new HealthCheckResult("cloudinary_config", "Healthy", 0);
    }

    private static HealthCheckResult CheckArtifactObjectStorageConfig(IConfiguration configuration)
    {
        var endpoint = configuration["RobotArtifacts:ObjectStorage:Endpoint"];
        var accessKey = configuration["RobotArtifacts:ObjectStorage:AccessKey"];
        var secretKey = configuration["RobotArtifacts:ObjectStorage:SecretKey"];
        var bucketName = configuration["RobotArtifacts:ObjectStorage:BucketName"];

        return IsMissingOrPlaceholder(endpoint) ||
               IsMissingOrPlaceholder(accessKey) ||
               IsMissingOrPlaceholder(secretKey) ||
               string.IsNullOrWhiteSpace(bucketName)
            ? new HealthCheckResult("artifact_object_storage_config", "Missing", 0,
                "Artifact object-storage config is missing or placeholder.")
            : new HealthCheckResult("artifact_object_storage_config", "Healthy", 0);
    }

    private static HealthCheckResult CheckClientRuntimeKeyRing(IConfiguration configuration)
    {
        var keyRingDirectory = configuration["ClientRuntime:OrderAccessKeyRingDirectory"];
        if (string.IsNullOrWhiteSpace(keyRingDirectory))
        {
            return new HealthCheckResult("client_order_key_ring", "Missing", 0,
                "Client order-token key-ring directory is not configured.");
        }

        try
        {
            if (!Directory.Exists(keyRingDirectory))
            {
                return new HealthCheckResult("client_order_key_ring", "Unhealthy", 0,
                    "Client order-token key-ring directory is unavailable.");
            }

            _ = Directory.EnumerateFileSystemEntries(keyRingDirectory).Take(1).ToArray();
            return new HealthCheckResult("client_order_key_ring", "Healthy", 0);
        }
        catch (Exception)
        {
            return new HealthCheckResult("client_order_key_ring", "Unhealthy", 0,
                "Client order-token key-ring directory cannot be read.");
        }
    }

    private static HealthCheckResult CheckClientDeviceSecurityConfig(IConfiguration configuration)
    {
        var currentHashKeyVersion = configuration["ClientDevices:Security:CurrentHashKeyVersion"];
        var currentHashKey = string.IsNullOrWhiteSpace(currentHashKeyVersion)
            ? null
            : configuration[$"ClientDevices:Security:HashKeys:{currentHashKeyVersion}"];
        var jwtSecret = configuration["ClientDevices:Security:JwtSecret"];
        var issuer = configuration["ClientDevices:Security:Issuer"];
        var audience = configuration["ClientDevices:Security:Audience"];

        return string.IsNullOrWhiteSpace(currentHashKeyVersion) ||
               IsMissingOrPlaceholder(currentHashKey) ||
               IsMissingOrPlaceholder(jwtSecret) ||
               jwtSecret!.Length < 32 ||
               string.IsNullOrWhiteSpace(issuer) ||
               string.IsNullOrWhiteSpace(audience)
            ? new HealthCheckResult("client_device_security", "Missing", 0,
                "Client-device credential or JWT config is incomplete.")
            : new HealthCheckResult("client_device_security", "Healthy", 0);
    }

    private static async Task<HealthCheckResult> CheckPayOsRealtimeAsync(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var baseUrl = configuration["PayOS:BaseUrl"];

        if (IsMissingOrPlaceholder(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return new HealthCheckResult("payos_realtime", "Skipped", 0, "PayOS base URL is not configured.");
        }

        return await CheckExternalProbeAsync(
            "payos_realtime",
            "PayOS realtime ping failed.",
            timeoutSeconds,
            cancellationToken,
            async timeoutToken =>
            {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var client = httpClientFactory.CreateClient();
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutToken);
            response.Dispose();
            });
    }

    private static async Task<HealthCheckResult> CheckCloudinaryRealtimeAsync(
        ICatalogImageStorageHealthProbe catalogImageStorageHealthProbe,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        return await CheckExternalProbeAsync(
            "cloudinary_realtime",
            "Cloudinary API ping failed.",
            timeoutSeconds,
            cancellationToken,
            catalogImageStorageHealthProbe.EnsureReadyAsync);
    }

    private static async Task<HealthCheckResult> CheckArtifactObjectStorageRealtimeAsync(
        IArtifactObjectStorage artifactObjectStorage,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        return await CheckExternalProbeAsync(
            "artifact_object_storage_realtime",
            "Artifact object storage ping failed.",
            timeoutSeconds,
            cancellationToken,
            artifactObjectStorage.EnsureReadyAsync);
    }

    private static async Task<HealthCheckResult> CheckExternalProbeAsync(
        string checkName,
        string failureReason,
        int timeoutSeconds,
        CancellationToken cancellationToken,
        Func<CancellationToken, Task> probe)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var timeoutCts = CreateTimeoutCancellationToken(timeoutSeconds, cancellationToken);
            await probe(timeoutCts.Token);
            stopwatch.Stop();
            return new HealthCheckResult(checkName, "Healthy", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception)
        {
            stopwatch.Stop();
            return new HealthCheckResult(checkName, "Unhealthy", stopwatch.ElapsedMilliseconds, failureReason);
        }
    }

    private static string DetermineOverallStatus(IEnumerable<HealthCheckResult> checks)
    {
        if (checks.Any(check => string.Equals(check.Status, "Unhealthy", StringComparison.OrdinalIgnoreCase)))
        {
            return "Unhealthy";
        }

        if (checks.Any(check =>
                string.Equals(check.Status, "Missing", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(check.Status, "Degraded", StringComparison.OrdinalIgnoreCase)))
        {
            return "Degraded";
        }

        return "Healthy";
    }

    private static bool IsMissingOrPlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("example.com", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("smtp-user", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("smtp-password", StringComparison.OrdinalIgnoreCase);
    }

    private static SecureSocketOptions ResolveSmtpSecureSocketOptions(IConfiguration configuration, int port)
    {
        var enableSsl = configuration.GetValue<bool?>("Email:EnableSsl") ?? true;
        if (!enableSsl)
        {
            return SecureSocketOptions.Auto;
        }

        return port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }

    private static bool IsExpectedFirebaseProbeMiss(FirebaseAuthException exception)
    {
        var message = exception.Message ?? string.Empty;
        return message.Contains("USER_NOT_FOUND", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("no user record", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("cannot find", StringComparison.OrdinalIgnoreCase);
    }

    private static CancellationTokenSource CreateTimeoutCancellationToken(
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        return timeoutCts;
    }
}

public sealed record HealthCheckResult(
    string Name,
    string Status,
    long DurationMs,
    string? Reason = null
);

public sealed record HealthResponse(
    string Status,
    IReadOnlyList<HealthCheckResult> Checks,
    long DurationMs,
    DateTimeOffset CheckedAt
);
