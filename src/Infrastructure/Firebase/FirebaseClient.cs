using Application.Shared.Exceptions;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Firebase;

public class FirebaseClient : IFirebaseClient
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<FirebaseClient> _logger;
    private FirebaseApp? _app;

    public FirebaseClient(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<FirebaseClient> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public FirebaseAuth GetAuth()
    {
        return FirebaseAuth.GetAuth(GetOrCreateApp());
    }

    public FirebaseMessaging GetMessaging()
    {
        return FirebaseMessaging.GetMessaging(GetOrCreateApp());
    }

    private FirebaseApp GetOrCreateApp()
    {
        if (_app is not null)
        {
            return _app;
        }

        if (FirebaseApp.DefaultInstance is not null)
        {
            _app = FirebaseApp.DefaultInstance;
            return _app;
        }

        var enabled = _configuration.GetSection("Firebase").GetValue<bool?>("Enabled") ?? true;
        if (!enabled)
        {
            throw new AppException("Firebase integration is disabled.", 503);
        }

        try
        {
            var inlineCredentials = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");
            GoogleCredential credential;

            if (!string.IsNullOrWhiteSpace(inlineCredentials))
            {
                using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(inlineCredentials));
                credential = CredentialFactory.FromStream<ServiceAccountCredential>(stream).ToGoogleCredential();
            }
            else
            {
                var credentialsPath =
                    _configuration.GetSection("Firebase").GetValue<string>("CredentialsPath")
                    ?? "../Infrastructure/Firebase/icecream-arm-robot-firebase-adminsdk-fbsvc-d729c976e7.json";

                if (!Path.IsPathRooted(credentialsPath))
                {
                    credentialsPath = Path.Combine(_environment.ContentRootPath, credentialsPath);
                }

                if (!File.Exists(credentialsPath))
                {
                    throw new AppException("Firebase integration is unavailable because credentials are not configured.", 503);
                }

                credential = CredentialFactory.FromFile<ServiceAccountCredential>(credentialsPath).ToGoogleCredential();
            }

            _app = FirebaseApp.Create(new AppOptions { Credential = credential });
            return _app;
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Firebase initialization failed.");
            throw new AppException($"Firebase initialization failed: {ex.Message}", 503);
        }
    }
}
