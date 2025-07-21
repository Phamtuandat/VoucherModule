using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace AuthenticationAPI.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings _settings;

        public TokenService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public string GenerateToken(string userId, string userName, UserRole role, DateTime expirationDate)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, role.ToString()),
                new Claim("scope", role == UserRole.Admin ? "voucher:write voucher:read voucher:apply read:users" : "voucher:read")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expirationDate,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool ValidateToken(string token, out string userId, out string userName, out string role)
        {
            userId = string.Empty;
            userName = string.Empty;
            role = string.Empty;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_settings.Key);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _settings.Issuer,
                    ValidAudience = _settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "";
                userName = principal.FindFirst(ClaimTypes.Name)?.Value ?? "";
                role = principal.FindFirst(ClaimTypes.Role)?.Value ?? "";

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void InvalidateToken(string token)
        {
            // Gợi ý: Có thể lưu vào danh sách blacklist trong cache/db
            // Tạm thời chưa làm gì
        }

        public DateTime GetTokenExpirationDate(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }

        public bool IsTokenValid(string token)
        {
            return ValidateToken(token, out _, out _, out _);
        }
    }
}
