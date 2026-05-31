namespace Application.Identity.Abstractions
{
    public interface IExternalIdentityProvider
    {
        string ProviderName { get; }
        Task<ExternalAuthUser> ValidateTokenAsync(string idToken, CancellationToken cancellationToken = default);
    }

    public record ExternalAuthUser(
        string Email,
        bool EmailVerified,
        string ExternalId,
        string? FullName,
        string? PreferredUserName);
}
