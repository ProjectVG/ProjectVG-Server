using ProjectVG.Application.Models.User;
using ProjectVG.Common.Models;

namespace ProjectVG.Application.Services.Auth
{
    /// <summary>
    /// 인증 및 토큰 관리 서비스
    /// JWT 토큰 생성, 검증, 갱신 및 OAuth 로그인 처리를 담당
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// OAuth 제공자를 통한 로그인 처리 (Google, GitHub, Microsoft, 게스트 등)
        /// </summary>
        /// <param name="provider">인증 제공자 (google, github, microsoft, guest, test)</param>
        /// <param name="providerUserId">제공자별 사용자 ID</param>
        /// <summary>
/// 지정된 OAuth 공급자 정보로 비동기 로그인 수행하여 토큰과 사용자 정보를 반환합니다.
/// </summary>
/// <param name="provider">OAuth 공급자 식별자(예: "google", "github", "microsoft", "guest", "test").</param>
/// <param name="providerUserId">해당 공급자에서 발급한 사용자 고유 ID(공급자별 식별자).</param>
/// <returns>액세스/리프레시 토큰 및 로그인된 사용자 정보를 포함한 AuthResult.</returns>
        Task<AuthResult> LoginWithOAuthAsync(string provider, string providerUserId);

        /// <summary>
        /// 리프레시 토큰을 사용하여 새로운 액세스 토큰 발급
        /// </summary>
        /// <param name="refreshToken">유효한 리프레시 토큰</param>
        /// <summary>
/// 주어진 리프레시 토큰으로 새 액세스/리프레시 토큰 쌍을 발급하고 관련 사용자 정보를 반환합니다.
/// </summary>
/// <param name="refreshToken">토큰 갱신에 사용할 리프레시 토큰(널일 수 있음).</param>
/// <returns>발급된 토큰 정보(TokenResponse)와 사용자 정보(UserDto)를 포함한 AuthResult.</returns>
        Task<AuthResult> RefreshTokenAsync(string? refreshToken);

        /// <summary>
        /// 사용자 로그아웃 처리 (리프레시 토큰 무효화)
        /// </summary>
        /// <param name="refreshToken">무효화할 리프레시 토큰</param>
        /// <summary>
/// 제공된 리프레시 토큰을 무효화하여 로그아웃을 수행합니다.
/// </summary>
/// <param name="refreshToken">무효화할 리프레시 토큰(없을 수 있음).</param>
/// <returns>로그아웃(토큰 무효화) 성공 시 true, 실패 시 false.</returns>
        Task<bool> LogoutAsync(string? refreshToken);
    }

    /// <summary>
    /// 인증 처리 결과를 담는 클래스
    /// </summary>
    public class AuthResult
    {
        /// <summary>JWT 토큰 정보</summary>
        public TokenResponse Tokens { get; set; } = null!;
        
        /// <summary>사용자 정보</summary>
        public UserDto User { get; set; } = null!;
    }
}
