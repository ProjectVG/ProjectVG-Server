using ProjectVG.Application.Models.User;
using ProjectVG.Common.Models;

namespace ProjectVG.Application.Services.Auth
{
    /// <summary>
    /// 인증 및 토큰 관리 서비스
    /// JWT 토큰 생성, 검증, 갱신 및 OAuth 로그인 처리를 담당
    /// </summary>
    public interface IUserAuthService
    {
        /// <summary>
        /// OAuth 제공자를 통한 로그인 처리
        /// </summary>
        Task<AuthResult> SignInWithOAuthAsync(string provider, string providerUserId);

        /// <summary>
        /// 리프레시 토큰을 사용하여 새로운 액세스 토큰 발급
        /// </summary>
        Task<AuthResult> RefreshAccessTokenAsync(string? refreshToken);

        /// <summary>
        /// 사용자 로그아웃 처리 (리프레시 토큰 무효화)
        /// </summary>
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
