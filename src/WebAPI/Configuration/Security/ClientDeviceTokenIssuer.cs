using Application.ClientDevices.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebAPI.Configuration.Security;

public sealed class ClientDeviceTokenIssuer(IOptions<ClientDeviceSecurityOptions> options) : IClientDeviceTokenIssuer
{
    private readonly ClientDeviceSecurityOptions _options = options.Value;

    public string Issue(Guid clientDeviceId, Guid kioskId, int credentialVersion, int sessionVersion)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, $"client-device:{clientDeviceId:D}"),
                new Claim(ClientDeviceAuthenticationDefaults.ClientDeviceIdClaim, clientDeviceId.ToString("D")),
                new Claim(ClientDeviceAuthenticationDefaults.KioskIdClaim, kioskId.ToString("D")),
                new Claim(ClientDeviceAuthenticationDefaults.CredentialVersionClaim, credentialVersion.ToString()),
                new Claim(ClientDeviceAuthenticationDefaults.SessionVersionClaim, sessionVersion.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ]),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            NotBefore = now,
            Expires = now.AddMinutes(_options.TokenLifetimeMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSecret)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(descriptor));
    }
}
