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
