using Application.Identity.Abstractions;
using Application.Shared.Exceptions;
using Infrastructure.Firebase;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity.ExternalAuth
{
    public class FirebaseExternalIdentityProvider : IExternalIdentityProvider
    {
        private readonly IFirebaseClient _firebaseClient;
        private readonly ILogger<FirebaseExternalIdentityProvider> _logger;

        public FirebaseExternalIdentityProvider(
            IFirebaseClient firebaseClient,
            ILogger<FirebaseExternalIdentityProvider> logger)
        {
            _firebaseClient = firebaseClient;
            _logger = logger;
        }

        public string ProviderName => "Google";

        public async Task<ExternalAuthUser> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default)
        {
            var firebaseAuth = _firebaseClient.GetAuth();

            try
            {
                var decoded = await firebaseAuth.VerifyIdTokenAsync(idToken, cancellationToken);
                var email = decoded.Claims.TryGetValue("email", out var emailObj) ? emailObj?.ToString() ?? string.Empty : string.Empty;
                var verified = decoded.Claims.TryGetValue("email_verified", out var verifiedObj) && Convert.ToBoolean(verifiedObj);
                var fullName = decoded.Claims.TryGetValue("name", out var nameObj) ? nameObj?.ToString() : null;
                var preferred = decoded.Claims.TryGetValue("preferred_username", out var preferredObj) ? preferredObj?.ToString() : null;

                return new ExternalAuthUser(email, verified, decoded.Uid, fullName, preferred);
            }
            catch (AppException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate Firebase token.");
                throw new AppException($"Firebase token validation failed: {ex.Message}", 503);
            }
        }

    }
}
