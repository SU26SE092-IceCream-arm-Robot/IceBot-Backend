using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace WebAPI.Configuration.Security;

public interface IPublicOrderAccessTokenService
{
    string Issue(Guid orderId, Guid kioskId);
    bool CanAccess(string? token, Guid orderId);
}

public sealed class PublicOrderAccessTokenService : IPublicOrderAccessTokenService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);
    private readonly ITimeLimitedDataProtector _protector;

    public PublicOrderAccessTokenService(IDataProtectionProvider provider)
    {
        _protector = provider
            .CreateProtector("IceBot.PublicOrderAccess.v1")
            .ToTimeLimitedDataProtector();
    }

    public string Issue(Guid orderId, Guid kioskId) =>
        _protector.Protect($"{orderId:N}|{kioskId:N}", Lifetime);

    public bool CanAccess(string? token, Guid orderId)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var payload = _protector.Unprotect(token, out _);
            var separator = payload.IndexOf('|');
            return separator > 0 &&
                Guid.TryParseExact(payload[..separator], "N", out var tokenOrderId) &&
                tokenOrderId == orderId;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
