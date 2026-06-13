using Infrastructure.Data;
using Infrastructure.Firebase;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;

namespace WebAPI.Configuration;

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
            checks.Add(CheckFirebaseConfig(configuration));
            checks.Add(CheckPayOsConfig(configuration));

            if (configuration.GetValue<bool>("Diagnostics:EnableExternalPing"))
            {
                var timeoutSeconds = Math.Clamp(
                    configuration.GetValue<int?>("Diagnostics:ExternalPingTimeoutSeconds") ?? 5,
                    1,
                    30);

                checks.Add(await CheckSmtpRealtimeAsync(configuration, timeoutSeconds, cancellationToken));
                checks.Add(await CheckFirebaseRealtimeAsync(configuration, firebaseClient, timeoutSeconds, cancellationToken));
                checks.Add(await CheckPayOsRealtimeAsync(configuration, httpClientFactory, timeoutSeconds, cancellationToken));
            }
            else
            {
                checks.Add(new HealthCheckResult("external_ping", "Skipped", 0, "Realtime SMTP/Firebase/PayOS ping is disabled."));
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

        try
        {
            using var timeoutCts = CreateTimeoutCancellationToken(timeoutSeconds, cancellationToken);
            using var client = new SmtpClient();
            var secureSocketOptions = ResolveSmtpSecureSocketOptions(configuration, port);
            var smtpHost = host!;
            var smtpUserName = userName!;
            var smtpPassword = password!;

            await client.ConnectAsync(smtpHost, port, secureSocketOptions, timeoutCts.Token);
            await client.AuthenticateAsync(smtpUserName, smtpPassword, timeoutCts.Token);
            await client.DisconnectAsync(true, timeoutCts.Token);

            stopwatch.Stop();
            return new HealthCheckResult("smtp_realtime", "Healthy", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception)
        {
            stopwatch.Stop();
            return new HealthCheckResult("smtp_realtime", "Unhealthy", stopwatch.ElapsedMilliseconds, "SMTP realtime ping failed.");
        }
    }

    private static HealthCheckResult CheckFirebaseConfig(IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool?>("Firebase:Enabled") ?? true;
        if (!enabled)
        {
            return new HealthCheckResult("firebase_config", "Disabled", 0, "Firebase integration is disabled.");
        }

        var googleCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        var configuredPath = configuration["Firebase:CredentialsPath"];
        var fallbackPath = "../Infrastructure/Firebase/icecream-arm-robot-firebase-adminsdk-fbsvc-d729c976e7.json";

        if (File.Exists(googleCredentials) ||
            File.Exists(configuredPath) ||
            File.Exists(fallbackPath))
        {
            return new HealthCheckResult("firebase_config", "Healthy", 0);
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

        try
        {
            using var timeoutCts = CreateTimeoutCancellationToken(timeoutSeconds, cancellationToken);
            var auth = firebaseClient.GetAuth();

            try
            {
                await auth.GetUserAsync("__icebot_diagnostics_probe__", timeoutCts.Token);
            }
            catch (FirebaseAuthException ex) when (IsExpectedFirebaseProbeMiss(ex))
            {
                stopwatch.Stop();
                return new HealthCheckResult("firebase_realtime", "Healthy", stopwatch.ElapsedMilliseconds);
            }

            stopwatch.Stop();
            return new HealthCheckResult("firebase_realtime", "Healthy", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception)
        {
            stopwatch.Stop();
            return new HealthCheckResult("firebase_realtime", "Unhealthy", stopwatch.ElapsedMilliseconds, "Firebase realtime ping failed.");
        }
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

    private static async Task<HealthCheckResult> CheckPayOsRealtimeAsync(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var baseUrl = configuration["PayOS:BaseUrl"];

        if (IsMissingOrPlaceholder(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return new HealthCheckResult("payos_realtime", "Skipped", stopwatch.ElapsedMilliseconds, "PayOS base URL is not configured.");
        }

        try
        {
            using var timeoutCts = CreateTimeoutCancellationToken(timeoutSeconds, cancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var client = httpClientFactory.CreateClient();
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
            response.Dispose();

            stopwatch.Stop();
            return new HealthCheckResult("payos_realtime", "Healthy", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception)
        {
            stopwatch.Stop();
            return new HealthCheckResult("payos_realtime", "Unhealthy", stopwatch.ElapsedMilliseconds, "PayOS realtime ping failed.");
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
