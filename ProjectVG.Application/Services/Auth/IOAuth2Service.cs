using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Application.Services.Auth
{
    public interface IOAuth2Service
    {
        Task<string> BuildAuthorizationUrlAsync(string scope, string state, string codeChallenge, string codeChallengeMethod, string codeVerifier, string clientRedirectUri);
        Task<OAuth2CallbackResult> HandleOAuth2CallbackAsync(string code, string state);
        Task<OAuth2TokenData?> GetTokenDataAsync(string state);
        Task DeleteTokenDataAsync(string state);
        Task<TokenResponse> ExchangeAuthorizationCodeAsync(string code, string clientId, string redirectUri, string codeVerifier = "");
        Task<OAuth2UserInfo> GetUserInfoAsync(string accessToken, string provider);
        Task<OAuth2AuthRequest> StoreOAuth2RequestAsync(string state, OAuth2AuthRequest request);
        Task<OAuth2AuthRequest?> GetOAuth2RequestAsync(string state);
        Task DeleteOAuth2RequestAsync(string state);
        Task StoreTokenDataAsync(string state, object tokenData);
    }

    public class OAuth2UserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Picture { get; set; }
        public string Provider { get; set; } = string.Empty;
    }
}
