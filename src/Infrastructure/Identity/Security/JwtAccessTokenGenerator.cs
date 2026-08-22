using Application.Identity.Abstractions;
using Application.Identity.Tokens.Claims;
using Application.Shared.Wrappers;
using Domain.Identity.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Identity.Security
{
    public class JwtAccessTokenGenerator : IAccessTokenGenerator
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtAccessTokenGenerator> _logger;

        public JwtAccessTokenGenerator(
            IConfiguration configuration,
            ILogger<JwtAccessTokenGenerator> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public ApiResult<string> GenerateAccessToken(
            Guid accountId,
            Guid sessionId,
            string accountUserName,
            IReadOnlyCollection<AccountRoleClaim> roles,
            AccountStatus accountStatus,
            long authorizationVersion)
        {
            if (roles.Count == 0)
            {
                _logger.LogWarning("Attempted to generate JWT token without roles.");
                return ApiResult<string>.Fail("At least one role is required.");
            }

            var secret = _configuration["Authentication:Jwt:Secret"];
            var issuer = _configuration["Authentication:Jwt:Issuer"];
            var audience = _configuration["Authentication:Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            {
                _logger.LogError("JWT configuration is incomplete.");
                return ApiResult<string>.Fail("JWT configuration is missing or invalid.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, accountId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, accountId.ToString()),
                new("session_id", sessionId.ToString()),
                new(ClaimTypes.Name, accountUserName),
                new("account_status", accountStatus.ToString()),
                new("authorization_version", authorizationVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.RoleCode));
                claims.Add(new Claim("role_scope", FormatRoleScope(role)));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials);

            return ApiResult<string>.Success(new JwtSecurityTokenHandler().WriteToken(token));
        }

        private static string FormatRoleScope(AccountRoleClaim role)
        {
            return string.Join(
                "|",
                role.RoleCode,
                role.OrganizationId?.ToString() ?? "*",
                role.StoreId?.ToString() ?? "*",
                role.KioskId?.ToString() ?? "*");
        }
    }
}
