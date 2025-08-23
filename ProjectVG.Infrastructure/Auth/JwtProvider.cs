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

        public string GenerateAccessToken(Guid userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtKey);
            
            // 디버그: 토큰 생성 정보 출력
            Console.WriteLine($"=== Access Token 생성 디버그 ===");
            Console.WriteLine($"JWT Key 길이: {_jwtKey.Length}");
            Console.WriteLine($"JWT Key 미리보기: {_jwtKey.Substring(0, Math.Min(10, _jwtKey.Length))}...");
            Console.WriteLine($"Issuer: {_issuer}");
            Console.WriteLine($"Audience: {_audience}");
            Console.WriteLine($"User ID: {userId}");
            Console.WriteLine($"만료 시간: {_accessTokenExpirationMinutes}분");
            
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
            
            Console.WriteLine($"생성된 토큰 길이: {tokenString.Length}");
            Console.WriteLine($"생성된 토큰 미리보기: {tokenString.Substring(0, Math.Min(20, tokenString.Length))}...");
            
            return tokenString;
        }

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

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtKey);

            // 디버그: JWT 설정 정보 출력
            Console.WriteLine($"=== JWT 검증 디버그 ===");
            Console.WriteLine($"JWT Key 길이: {_jwtKey.Length}");
            Console.WriteLine($"JWT Key 미리보기: {_jwtKey.Substring(0, Math.Min(10, _jwtKey.Length))}...");
            Console.WriteLine($"Issuer: {_issuer}");
            Console.WriteLine($"Audience: {_audience}");
            Console.WriteLine($"토큰 길이: {token.Length}");

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
                Console.WriteLine("JWT 검증 성공!");
                return principal;
            }
            catch (Exception ex)
            {
                // 디버그를 위해 예외 정보 로깅
                Console.WriteLine($"JWT 검증 실패: {ex.Message}");
                Console.WriteLine($"예외 타입: {ex.GetType().Name}");
                return null;
            }
        }

        public string? GetUserIdFromToken(string token)
        {
            Console.WriteLine($"=== GetUserIdFromToken 디버그 ===");
            Console.WriteLine($"토큰 길이: {token.Length}");
            
            var principal = ValidateToken(token);
            Console.WriteLine($"ValidateToken 결과: {(principal != null ? "성공" : "실패")}");
            
            if (principal != null)
            {
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"추출된 UserId: {userId}");
                return userId;
            }
            else
            {
                Console.WriteLine("ValidateToken이 null을 반환하여 UserId 추출 실패");
                return null;
            }
        }
    }
}
