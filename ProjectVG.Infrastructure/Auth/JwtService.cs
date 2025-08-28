using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ProjectVG.Infrastructure.Auth
{
    public class JwtService
    {
        private readonly string _secretKey;

        /// <summary>
        /// JwtService 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="secretKey">JWT 서명에 사용할 대칭 비밀키(UTF-8로 인코딩하여 HMAC-SHA256 서명에 사용). 유효한 비밀키 문자열이어야 합니다.</param>
        public JwtService(string secretKey)
        {
            _secretKey = secretKey;
        }

        /// <summary>
        /// 지정된 사용자 ID를 기반으로 서명된 JWT(JSON Web Token)를 생성하여 반환합니다.
        /// </summary>
        /// <param name="userId">토큰의 NameIdentifier 클레임으로 포함할 사용자 식별자(예: 사용자 ID).</param>
        /// <returns>HMAC-SHA256로 서명되고 발행 시점으로부터 30분간 유효한 JWT 문자열.</returns>
        public string GenerateJwt(string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }),
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}


