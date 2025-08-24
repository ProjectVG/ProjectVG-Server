using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Application.Services.Auth
{
    public interface IOAuth2Service
    {
        Task<TokenResponse> ExchangeAuthorizationCodeAsync(string code, string clientId, string redirectUri, string codeVerifier = "");
        Task<TokenResponse> RefreshAccessTokenAsync(string refreshToken, string clientId);
        Task<OAuth2UserInfo> GetUserInfoAsync(string accessToken, string provider);
        Task<OAuth2AuthRequest> StoreOAuth2RequestAsync(string state, OAuth2AuthRequest request);
        Task<OAuth2AuthRequest?> GetOAuth2RequestAsync(string state);
        Task DeleteOAuth2RequestAsync(string state);
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
