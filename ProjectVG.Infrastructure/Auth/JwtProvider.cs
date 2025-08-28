using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ProjectVG.Infrastructure.Auth
{
    public interface IJwtProvider
    {
        /// <summary>
/// 주어진 사용자 ID로 액세스용 JWT를 생성하여 직렬화된 토큰 문자열을 반환합니다.
/// </summary>
/// <param name="userId">토큰에 포함할 사용자 식별자(Guid).</param>
/// <returns>생성된 액세스 토큰의 직렬화된 문자열. 토큰에는 NameIdentifier 클레임(사용자 ID)과 `token_type="access"`가 포함됩니다.</returns>
string GenerateAccessToken(Guid userId);
        /// <summary>
/// 지정된 사용자 ID에 대한 만료된 리프레시 토큰(서명된 JWT)을 생성합니다.
/// </summary>
/// <param name="userId">토큰에 포함할 사용자 식별자(Guid).</param>
/// <returns>
/// 서명된 JWT 문자열. 토큰에는 NameIdentifier 클레임(userId)과 "refresh"인 token_type 클레임이 포함되며,
/// 제공된 설정에 따른 발급자·대상자·리프레시 토큰 만료 시간이 적용됩니다.
/// </returns>
string GenerateRefreshToken(Guid userId);
        /// <summary>
/// 주어진 JWT 문자열의 서명, 발행자, 대상, 유효기간을 검증하고 검증에 성공하면 해당 토큰의 클레임을 담은 <see cref="ClaimsPrincipal"/>을 반환합니다.
/// </summary>
/// <param name="token">검증할 JWT 문자열(액세스 토큰 또는 리프레시 토큰).</param>
/// <returns>토큰이 유효하면 해당 토큰의 <see cref="ClaimsPrincipal"/>; 유효하지 않거나 검증 실패 시 <c>null</c>.</returns>
ClaimsPrincipal? ValidateToken(string token);
        /// <summary>
/// JWT에서 사용자 ID(NameIdentifier 클레임)를 추출하여 반환합니다.
/// </summary>
/// <param name="token">검증할 JWT 문자열.</param>
/// <returns>토큰이 유효하고 NameIdentifier 클레임이 존재하면 해당 사용자 ID 문자열을 반환하고, 그렇지 않으면 null을 반환합니다.</returns>
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

        /// <summary>
        /// JwtProvider 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="jwtKey">토큰 서명에 사용할 비밀 키(대칭 키 문자열).</param>
        /// <param name="issuer">발급자(issuer)로 사용될 식별자.</param>
        /// <param name="audience">토큰의 대상(audience)으로 사용할 식별자.</param>
        /// <param name="accessTokenExpirationMinutes">액세스 토큰 만료 시간(분 단위).</param>
        /// <param name="refreshTokenExpirationMinutes">리프레시 토큰 만료 시간(분 단위).</param>
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
        /// <summary>
        /// 지정한 사용자 ID로 만료 시간과 발급자/대상자를 포함한 액세스 JWT를 생성하여 직렬화된 문자열로 반환합니다.
        /// </summary>
        /// <param name="userId">토큰에 포함될 사용자 식별자(ClaimTypes.NameIdentifier로 저장됨).</param>
        /// <returns>생성된 액세스 토큰의 직렬화된 JWT 문자열(내부적으로 `token_type` 클레임은 "access"로 설정되고 설정된 만료 시간이 적용됨).</returns>
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
        /// <summary>
        /// 지정한 사용자 ID로 만료 시간이 설정된 refresh JWT를 생성합니다.
        /// </summary>
        /// <param name="userId">토큰이 발급될 사용자의 GUID (토큰의 NameIdentifier 클레임에 저장됨).</param>
        /// <returns>생성된 refresh 토큰 문자열 (토큰에는 `token_type` 클레임이 "refresh"로 설정되어 있음).</returns>
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
        /// <summary>
        /// 지정된 JWT를 검증하고 해당 토큰의 클레임을 포함한 ClaimsPrincipal을 반환합니다.
        /// </summary>
        /// <param name="token">검증할 JWT 문자열.</param>
        /// <returns>토큰이 유효하면 토큰의 클레임을 포함한 <see cref="ClaimsPrincipal"/>; 유효하지 않거나 검증에 실패하면 <c>null</c>.</returns>
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
        /// <summary>
        /// JWT에서 사용자 ID(NameIdentifier 클레임)를 추출하여 반환합니다.
        /// </summary>
        /// <param name="token">검증할 JWT 문자열(액세스 또는 리프레시 토큰).</param>
        /// <returns>토큰이 유효하고 NameIdentifier 클레임이 있으면 해당 사용자 ID 문자열, 그렇지 않으면 <c>null</c>.</returns>
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
