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
