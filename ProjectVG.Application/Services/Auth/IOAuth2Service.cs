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
        /// <summary>
/// 지정된 OAuth2 제공자와 PKCE 매개변수를 사용해 인증용 URL을 생성합니다.
/// </summary>
/// <param name="providerName">사용할 OAuth2 제공자 식별자(예: "Google", "Apple").</param>
/// <param name="state">CSRF 보호 및 요청 식별을 위한 상태 문자열.</param>
/// <param name="codeChallenge">PKCE 흐름에서 전송할 코드 챌린지(주로 S256으로 해시된 값).</param>
/// <param name="codeChallengeMethod">코드 챌린지의 해시 방법(예: "S256").</param>
/// <param name="codeVerifier">선택적 PKCE 코드 검증기(일부 흐름에서 사용됨).</param>
/// <param name="clientRedirectUri">클라이언트가 인증 후 리디렉션될 URI.</param>
/// <returns>외부 제공자 인증을 시작할 수 있는 전체 OAuth2 인증 URL.</returns>
        Task<string> BuildAuthorizationUrlAsync(string providerName, string state, string codeChallenge, string codeChallengeMethod, string codeVerifier, string clientRedirectUri);
        
        /// <summary>
        /// OAuth2 콜백 처리 (인증 코드를 토큰으로 교환하고 사용자 로그인 처리)
        /// </summary>
        /// <param name="code">인증 코드</param>
        /// <param name="state">상태값</param>
        /// <summary>
/// OAuth2 콜백을 처리하여 인증 코드를 토큰으로 교환하고 사용자 로그인 흐름을 완료한 후 결과를 반환합니다.
/// </summary>
/// <param name="code">OAuth2 제공자가 전달한 일회성 인증 코드(authorization code).</param>
/// <param name="state">콜백 요청 시 전달된 상태 값(CSRF 방지용, 이전에 생성한 상태와 일치해야 함).</param>
/// <returns>콜백 처리 결과를 담은 <see cref="OAuth2CallbackResult"/>(성공/실패 상태 및 관련 데이터).</returns>
        Task<OAuth2CallbackResult> HandleOAuth2CallbackAsync(string code, string state);

        /// <summary>
        /// 상태값으로 저장된 OAuth2 토큰 데이터 조회
        /// </summary>
        /// <param name="state">상태값</param>
        /// <summary>
/// 주어진 상태값(state)에 연관된 임시 저장된 OAuth2 토큰 데이터를 비동기적으로 조회합니다.
/// </summary>
/// <param name="state">OAuth2 흐름에서 생성된 상태값(예: CSRF 방지용 식별자) — 이 값을 키로 토큰 데이터를 조회합니다.</param>
/// <returns>해당 상태에 저장된 OAuth2 토큰 데이터(OAuth2TokenData) 또는 존재하지 않으면 null.</returns>
        Task<OAuth2TokenData?> GetTokenDataAsync(string state);

        /// <summary>
        /// OAuth2 토큰 데이터 삭제 (사용 후 정리)
        /// </summary>
        /// <summary>
/// 주어진 상태값(state)에 연관된 임시 저장된 OAuth2 토큰 데이터를 삭제합니다.
/// </summary>
/// <param name="state">토큰 데이터를 조회·식별하는 고유 상태값(요청에서 사용된 state 문자열).</param>
Task DeleteTokenDataAsync(string state);
        Task DeleteTokenDataAsync(string state);

        /// <summary>
        /// OAuth2 인증 코드를 액세스 토큰으로 교환
        /// </summary>
        /// <param name="code">인증 코드</param>
        /// <param name="clientId">클라이언트 ID</param>
        /// <param name="redirectUri">리다이렉트 URI</param>
        /// <param name="codeVerifier">PKCE code verifier (선택적)</param>
        /// <summary>
/// 인증 코드(Authorization Code)를 액세스 토큰으로 교환합니다.
/// </summary>
/// <param name="code">OAuth2 제공자가 발급한 인증 코드.</param>
/// <param name="clientId">클라이언트 식별자(Client ID).</param>
/// <param name="redirectUri">토큰 교환 시 사용한 리다이렉트 URI(등록된 값과 일치해야 함).</param>
/// <param name="codeVerifier">PKCE 흐름에서 사용되는 코드 검증기(선택적).</param>
/// <returns>토큰 교환 결과를 담은 <see cref="TokenResponse"/> 객체.</returns>
        Task<TokenResponse> ExchangeAuthorizationCodeAsync(string code, string clientId, string redirectUri, string codeVerifier = "");
        
        /// <summary>
        /// OAuth2 제공자에서 사용자 정보 조회
        /// </summary>
        /// <param name="accessToken">OAuth2 액세스 토큰</param>
        /// <param name="provider">제공자명 (google, apple)</param>
        /// <summary>
/// 주어진 액세스 토큰으로 지정된 OAuth2 제공자에서 사용자 정보를 조회합니다.
/// </summary>
/// <param name="accessToken">OAuth2 액세스 토큰(유효한 토큰이어야 함).</param>
/// <param name="provider">사용자 정보를 조회할 OAuth2 제공자의 식별자(예: "Google", "Apple").</param>
/// <returns>조회된 사용자 정보(OAuth2UserInfo).</returns>
        Task<OAuth2UserInfo> GetUserInfoAsync(string accessToken, string provider);
        
        /// <summary>
        /// OAuth2 요청 정보 저장 (임시 저장소)
        /// </summary>
        /// <param name="state">상태값</param>
        /// <param name="request">OAuth2 요청 정보</param>
        /// <summary>
/// 주어진 상태값(state)에 대해 OAuth2 인증 요청 정보를 임시 저장소에 저장합니다.
/// </summary>
/// <param name="state">저장 키로 사용되는 상태값(예: CSRF 보호용 state).</param>
/// <param name="request">저장할 OAuth2 요청 정보.</param>
/// <returns>저장된 OAuth2 요청 정보(임시 저장소에 저장된 객체).</returns>
        Task<OAuth2AuthRequest> StoreOAuth2RequestAsync(string state, OAuth2AuthRequest request);
        
        /// <summary>
        /// 저장된 OAuth2 요청 정보 조회
        /// </summary>
        /// <param name="state">상태값</param>
        /// <summary>
/// 상태값(state)에 저장된 OAuth2 인증 요청 정보를 조회합니다.
/// </summary>
/// <param name="state">조회할 OAuth2 요청을 식별하는 상태 문자열.</param>
/// <returns>해당 상태에 저장된 OAuth2 요청 정보. 없으면 null을 반환합니다.</returns>
        Task<OAuth2AuthRequest?> GetOAuth2RequestAsync(string state);
        
        /// <summary>
        /// OAuth2 요청 정보 삭제 (사용 후 정리)
        /// </summary>
        /// <summary>
/// 지정된 상태값으로 임시 저장된 OAuth2 인증 요청 정보를 삭제합니다.
/// </summary>
/// <param name="state">삭제할 요청을 식별하는 상태값(콜백에 사용된 state)</param>
        Task DeleteOAuth2RequestAsync(string state);
        
        /// <summary>
        /// 토큰 데이터를 상태값과 함께 임시 저장
        /// </summary>
        /// <param name="state">상태값</param>
        /// <summary>
/// 주어진 상태값(state)에 연결하여 토큰 관련 데이터를 비동기적으로 임시 저장합니다.
/// </summary>
/// <param name="state">토큰 데이터를 조회하거나 삭제할 때 사용할 식별자(상태값).</param>
/// <param name="tokenData">임시로 저장할 토큰 정보(액세스 토큰, 리프레시 토큰 등 임의의 형태).</param>
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
