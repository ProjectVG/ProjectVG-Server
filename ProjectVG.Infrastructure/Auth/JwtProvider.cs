using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ProjectVG.Infrastructure.Auth
{
    public interface IJwtProvider
    {
        string GenerateAccessToken(Guid userId);
        string GenerateRefreshToken(Guid userId);
        ClaimsPrincipal? ValidateToken(string token);
        string? GetUserIdFromToken(string token);
    }

    /// <summary>
    /// JWT 토큰 생성 및 검증 로직
    /// </summary>
    public class JwtProvider : IJwtProvider
    {
        private readonly string _jwtKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpirationMinutes;
        private readonly int _refreshTokenExpirationMinutes;

        public JwtProvider(string jwtKey, string issuer, string audience, int accessTokenExpirationMinutes, int refreshTokenExpirationMinutes)
        {
            _jwtKey = jwtKey;
            _issuer = issuer;
            _audience = audience;
            _accessTokenExpirationMinutes = accessTokenExpirationMinutes;
            _refreshTokenExpirationMinutes = refreshTokenExpirationMinutes;
        }

        /// <summary>
        /// 액세스 토큰 생성
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>생성된 토큰</returns>
        public string GenerateAccessToken(Guid userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtKey);
            

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("token_type", "access")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            

            return tokenString;
        }

        /// <summary>
        /// 리프레시 토큰 생성
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>생성된 토큰</returns>
        public string GenerateRefreshToken(Guid userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtKey);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("token_type", "refresh")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_refreshTokenExpirationMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// 토큰 검증
        /// </summary>
        /// <param name="token">검증할 토큰</param>
        /// <returns>검증된 토큰의 클레임 정보</returns>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                return principal;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 토큰에서 사용자 ID 추출
        /// </summary>
        /// <param name="token">사용자 ID를 추출할 토큰</param>
        /// <returns>추출된 사용자 ID</returns>
        public string? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            
            if (principal != null)
            {
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return userId;
            }
            else
            {
                return null;
            }
        }
    }
}
