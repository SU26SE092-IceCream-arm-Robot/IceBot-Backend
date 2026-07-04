using Application.Identity.Abstractions;
using Application.Shared.Exceptions;
using Infrastructure.Firebase;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity.ExternalAuth
{
    public class FirebaseExternalIdentityProvider : IExternalIdentityProvider
    {
        private readonly IFirebaseClient _firebaseClient;
        private readonly ILogger<FirebaseExternalIdentityProvider> _logger;
        private readonly FirebaseAuthResiliencePipeline _resilience;

        public FirebaseExternalIdentityProvider(
            IFirebaseClient firebaseClient,
            FirebaseAuthResiliencePipeline resilience,
            ILogger<FirebaseExternalIdentityProvider> logger)
        {
            _firebaseClient = firebaseClient;
            _resilience = resilience;
            _logger = logger;
        }

        public string ProviderName => "Google";

        public async Task<ExternalAuthUser> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default)
        {
            try
            {
                var decoded = await _resilience.ExecuteAsync(
                    async token => await _firebaseClient.VerifyIdTokenAsync(idToken, token),
                    cancellationToken);
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
            catch (FirebaseAuthException ex) when (FirebaseAuthResiliencePipeline.IsInvalidToken(ex.AuthErrorCode))
            {
                _logger.LogInformation("Firebase token validation rejected an invalid, expired, or revoked token.");
                throw new AppException("Invalid Firebase token.", 401);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate Firebase token.");
                throw new AppException($"Firebase token validation failed: {ex.Message}", 503);
            }
        }

    }
}
