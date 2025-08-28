using System.Text.Json;
using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Application.Services.Auth.Providers
{
    /// <summary>
    /// Apple OAuth2 제공자 구현
    /// Apple Sign-In은 OAuth2 표준을 따르지만 몇 가지 특별한 요구사항이 있음
    /// </summary>
    public class AppleOAuth2Provider : IOAuth2Provider
    {
        public string ProviderName => "apple";

        public string TokenEndpoint => "https://appleid.apple.com/auth/token";

        public string UserInfoEndpoint => "https://appleid.apple.com/auth/userinfo";

        public string[] DefaultScopes => new[] { "name", "email" };

        /// <summary>
        /// Apple OAuth2 승인(authorization) 엔드포인트로 리디렉션할 수 있는 인증 URL을 생성합니다.
        /// </summary>
        /// <param name="clientId">Apple에 등록된 클라이언트(앱) 식별자.</param>
        /// <param name="redirectUri">인증 후 Apple이 결과를 전송할 리디렉션 URI.</param>
        /// <param name="state">CSRF 보호 및 상태 전달용 임의 문자열.</param>
        /// <param name="codeChallenge">PKCE 흐름의 코드 챌린지 값.</param>
        /// <param name="codeChallengeMethod">코드 챌린지 방법(예: "S256").</param>
        /// <param name="scopes">요청할 권한(예: "name", "email") 목록; 내부적으로 공백으로 결합되어 전송됩니다.</param>
        /// <returns>모든 매개변수를 URL 인코딩하여 구성한 Apple 인증(authorization) URL.</returns>
        public string BuildAuthorizationUrl(string clientId, string redirectUri, string state, string codeChallenge, string codeChallengeMethod, string[] scopes)
        {
            var scopeString = string.Join(" ", scopes);
            
            return $"https://appleid.apple.com/auth/authorize" +
                   $"?client_id={Uri.EscapeDataString(clientId)}" +
                   $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                   $"&response_type=code" +
                   $"&scope={Uri.EscapeDataString(scopeString)}" +
                   $"&state={Uri.EscapeDataString(state)}" +
                   $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                   $"&code_challenge_method={Uri.EscapeDataString(codeChallengeMethod)}" +
                   $"&response_mode=form_post";
        }

        /// <summary>
        /// JSON 응답을 파싱하여 OAuth2UserInfo 객체를 생성합니다.
        /// </summary>
        /// <param name="jsonResponse">Apple의 userinfo 또는 ID 토큰에서 디코딩한 JSON 문자열(최소한 "sub" 필드를 포함).</param>
        /// <returns>파싱된 사용자 정보를 담은 OAuth2UserInfo 객체(Provider는 "apple").</returns>
        public OAuth2UserInfo ParseUserInfo(string jsonResponse)
        {
            var userData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonResponse);
            
            return new OAuth2UserInfo
            {
                Id = userData!["sub"].GetString()!,
                Email = userData.ContainsKey("email") ? userData["email"].GetString() ?? "" : "",
                Provider = ProviderName
            };
        }
    }
}
