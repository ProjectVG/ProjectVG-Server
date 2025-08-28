using System.Text.Json;
using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Application.Services.Auth.Providers
{
    /// <summary>
    /// Google OAuth2 제공자 구현
    /// </summary>
    public class GoogleOAuth2Provider : IOAuth2Provider
    {
        public string ProviderName => "google";

        public string TokenEndpoint => "https://oauth2.googleapis.com/token";

        public string UserInfoEndpoint => "https://www.googleapis.com/oauth2/v2/userinfo";

        public string[] DefaultScopes => new[] { "openid", "email", "profile" };

        /// <summary>
        /// Google OAuth2 인증 요청용 Authorization URL을 생성합니다.
        /// </summary>
        /// <param name="clientId">OAuth 클라이언트 ID.</param>
        /// <param name="redirectUri">인증 후 리디렉션될 URI.</param>
        /// <param name="state">CSRF 보호를 위한 상태 값.</param>
        /// <param name="codeChallenge">PKCE 코드 챌린지 (base64/url-safe 또는 SHA256 기반값).</param>
        /// <param name="codeChallengeMethod">PKCE 코드 챌린지 방식 (예: "S256").</param>
        /// <param name="scopes">요청할 권한 범위들; 내부에서 공백으로 결합되어 전송됩니다.</param>
        /// <returns>Google OAuth2 인증 엔드포인트로의 완전한 URL 문자열(모든 파라미터는 URL 인코딩됨).</returns>
        public string BuildAuthorizationUrl(string clientId, string redirectUri, string state, string codeChallenge, string codeChallengeMethod, string[] scopes)
        {
            var scopeString = string.Join(" ", scopes);

            return $"https://accounts.google.com/o/oauth2/v2/auth" +
                   $"?client_id={Uri.EscapeDataString(clientId)}" +
                   $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                   $"&response_type=code" +
                   $"&scope={Uri.EscapeDataString(scopeString)}" +
                   $"&state={Uri.EscapeDataString(state)}" +
                   $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                   $"&code_challenge_method={Uri.EscapeDataString(codeChallengeMethod)}";
        }

        /// <summary>
        /// JSON 응답에서 Google 사용자 정보를 파싱하여 OAuth2UserInfo 객체로 반환합니다.
        /// </summary>
        /// <param name="jsonResponse">Google UserInfo 엔드포인트에서 반환된 JSON 문자열(객체 형태, 최소한 "id"와 "email" 필드가 문자열로 포함되어야 함).</param>
        /// <returns>파싱된 사용자 정보(OAuth2UserInfo). Id와 Email은 JSON의 각각 "id", "email" 값을 사용하고 Provider는 해당 프로바이더 이름으로 설정됩니다.</returns>
        /// <remarks>
        /// 전달된 jsonResponse가 유효한 JSON이 아니거나 기대하는 필드가 없거나 타입이 맞지 않으면 JsonException, KeyNotFoundException 또는 InvalidOperationException 등이 발생할 수 있습니다.
        /// </remarks>
        public OAuth2UserInfo ParseUserInfo(string jsonResponse)
        {
            var userData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonResponse);

            return new OAuth2UserInfo {
                Id = userData!["id"].GetString()!,
                Email = userData["email"].GetString()!,
                Provider = ProviderName
            };
        }
    }
}
