using System.Security.Cryptography;
using System.Text;

namespace Application.Identity.Invitations;

internal static class AccountInvitationTokenHelper
{
    public static string CreateToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
