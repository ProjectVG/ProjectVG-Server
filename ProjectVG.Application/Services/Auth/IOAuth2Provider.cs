using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Application.Services.Auth
{
    /// <summary>
    /// OAuth2 제공자 인터페이스
    /// 각 OAuth2 제공자(Google, GitHub, Microsoft 등)는 이 인터페이스를 구현
    /// </summary>
    public interface IOAuth2Provider
    {
        /// <summary>
        /// 제공자 이름 (google, github, microsoft 등)
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// OAuth2 인증 URL 생성
        /// </summary>
        /// <param name="clientId">클라이언트 ID</param>
        /// <param name="redirectUri">리다이렉트 URI</param>
        /// <param name="state">상태값</param>
        /// <param name="codeChallenge">PKCE code challenge</param>
        /// <param name="codeChallengeMethod">PKCE challenge 방법</param>
        /// <param name="scopes">요청할 스코프</param>
        /// <returns>인증 URL</returns>
        string BuildAuthorizationUrl(string clientId, string redirectUri, string state, string codeChallenge, string codeChallengeMethod, string[] scopes);

        /// <summary>
        /// 토큰 교환 엔드포인트 URL
        /// </summary>
        string TokenEndpoint { get; }

        /// <summary>
        /// 사용자 정보 조회 엔드포인트 URL
        /// </summary>
        string UserInfoEndpoint { get; }

        /// <summary>
        /// 기본 스코프 목록
        /// </summary>
        string[] DefaultScopes { get; }

        /// <summary>
        /// 사용자 정보 응답을 표준 형식으로 변환
        /// </summary>
        /// <param name="jsonResponse">제공자별 JSON 응답</param>
        /// <returns>표준화된 사용자 정보</returns>
        OAuth2UserInfo ParseUserInfo(string jsonResponse);
    }
}
