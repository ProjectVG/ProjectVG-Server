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
        /// <summary>
/// OAuth2 인증을 시작하기 위한 인증(authorization) URL을 생성하여 반환합니다.
/// </summary>
/// <param name="clientId">OAuth2 클라이언트 식별자.</param>
/// <param name="redirectUri">인증 완료 후 리다이렉트할 콜백 URI.</param>
/// <param name="state">CSRF 방지 및 상태 유지에 사용되는 임의 문자열.</param>
/// <param name="codeChallenge">PKCE 흐름에서 전송할 code challenge 값(없으면 null 또는 빈 문자열).</param>
/// <param name="codeChallengeMethod">PKCE의 해시 방법(e.g. "S256"). codeChallenge가 비어있으면 무시될 수 있음.</param>
/// <param name="scopes">요청할 권한 범위 목록(빈 배열이면 기본 스코프 사용 가능).</param>
/// <returns>사용자를 인증 서버로 안내할 전체 인증 URL 문자열.</returns>
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
        /// <summary>
/// 제공자별 JSON 응답을 파싱하여 표준화된 OAuth2UserInfo 객체로 변환합니다.
/// </summary>
/// <param name="jsonResponse">OAuth2 공급자가 반환한 사용자 정보의 JSON 문자열.</param>
/// <returns>표준화된 사용자 정보(OAuth2UserInfo).</returns>
        OAuth2UserInfo ParseUserInfo(string jsonResponse);
    }
}
