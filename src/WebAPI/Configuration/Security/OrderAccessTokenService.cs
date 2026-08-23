using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace WebAPI.Configuration.Security;

public interface IOrderAccessTokenService
{
    string Issue(Guid orderId, Guid clientDeviceId);
    bool CanAccess(string? token, Guid orderId, Guid clientDeviceId);
}

public sealed class OrderAccessTokenService : IOrderAccessTokenService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);
    private readonly ITimeLimitedDataProtector _protector;

    public OrderAccessTokenService(IDataProtectionProvider provider)
    {
        _protector = provider
            .CreateProtector("IceBot.ClientDeviceOrderAccess.v1")
            .ToTimeLimitedDataProtector();
    }

    public string Issue(Guid orderId, Guid clientDeviceId) =>
        _protector.Protect($"{orderId:N}|{clientDeviceId:N}", Lifetime);

    public bool CanAccess(string? token, Guid orderId, Guid clientDeviceId)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var payload = _protector.Unprotect(token, out _);
            var parts = payload.Split('|', StringSplitOptions.None);
            return parts.Length == 2 &&
                Guid.TryParseExact(parts[0], "N", out var tokenOrderId) && tokenOrderId == orderId &&
                Guid.TryParseExact(parts[1], "N", out var tokenClientDeviceId) && tokenClientDeviceId == clientDeviceId;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
