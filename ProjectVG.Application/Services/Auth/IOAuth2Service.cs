using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Application.Services.Auth
{
    /// <summary>
    /// OAuth2 인증 플로우 관리 서비스
    /// Google, Apple 등의 외부 OAuth2 제공자와의 인증 처리를 담당
    /// </summary>
    public interface IOAuth2Service
    {
        /// <summary>
        /// 특정 제공자로 OAuth2 인증 URL 생성 (PKCE 플로우 지원)
        /// </summary>
        /// <param name="providerName">제공자 이름 (google, apple)</param>
        /// <param name="state">CSRF 보호를 위한 상태값</param>
        /// <param name="codeChallenge">PKCE code challenge</param>
        /// <param name="codeChallengeMethod">PKCE challenge 방법 (S256)</param>
        /// <param name="codeVerifier">PKCE code verifier</param>
        /// <param name="clientRedirectUri">클라이언트 리다이렉트 URI</param>
        /// <returns>OAuth2 인증 URL</returns>
        Task<string> BuildAuthorizationUrlAsync(string providerName, string state, string codeChallenge, string codeChallengeMethod, string codeVerifier, string clientRedirectUri);
        
        /// <summary>
        /// OAuth2 콜백 처리 (인증 코드를 토큰으로 교환하고 사용자 로그인 처리)
        /// </summary>
        /// <param name="code">인증 코드</param>
        /// <param name="state">상태값</param>
        /// <returns>콜백 처리 결과</returns>
        Task<OAuth2CallbackResult> HandleOAuth2CallbackAsync(string code, string state);

        /// <summary>
        /// 상태값으로 저장된 OAuth2 토큰 데이터 조회
        /// </summary>
        /// <param name="state">상태값</param>
        /// <returns>토큰 데이터 (없으면 null)</returns>
        Task<OAuth2TokenData?> GetTokenDataAsync(string state);

        /// <summary>
        /// OAuth2 토큰 데이터 삭제 (사용 후 정리)
        /// </summary>
        /// <param name="state">삭제할 토큰 데이터의 상태값</param>
        Task DeleteTokenDataAsync(string state);

        /// <summary>
        /// 상태값으로 저장된 OAuth2 토큰 데이터를 원자적으로 소비 (조회 후 삭제)
        /// </summary>
        /// <param name="state">상태값</param>
        /// <returns>토큰 데이터 (없으면 null)</returns>
        Task<OAuth2TokenData?> ConsumeTokenDataAsync(string state);

        
        /// <summary>
        /// OAuth2 요청 정보 저장 (임시 저장소)
        /// </summary>
        /// <param name="state">상태값</param>
        /// <param name="request">OAuth2 요청 정보</param>
        /// <returns>저장된 요청 정보</returns>
        Task<OAuth2AuthRequest> StoreOAuth2RequestAsync(string state, OAuth2AuthRequest request);
        
        /// <summary>
        /// 저장된 OAuth2 요청 정보 조회
        /// </summary>
        /// <param name="state">상태값</param>
        /// <returns>요청 정보 (없으면 null)</returns>
        Task<OAuth2AuthRequest?> GetOAuth2RequestAsync(string state);
        
        /// <summary>
        /// OAuth2 요청 정보 삭제 (사용 후 정리)
        /// </summary>
        /// <param name="state">삭제할 요청의 상태값</param>
        Task DeleteOAuth2RequestAsync(string state);
        
        /// <summary>
        /// 토큰 데이터를 상태값과 함께 임시 저장
        /// </summary>
        /// <param name="state">상태값</param>
        /// <param name="tokenData">저장할 토큰 데이터</param>
        Task StoreTokenDataAsync(string state, object tokenData);
    }

    /// <summary>
    /// OAuth2 제공자에서 가져온 사용자 정보
    /// </summary>
    public class OAuth2UserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
    }
}
