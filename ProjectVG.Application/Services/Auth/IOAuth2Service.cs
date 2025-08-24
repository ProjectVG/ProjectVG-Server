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
        Task StoreTokenDataAsync(string state, object tokenData);
        Task<object?> GetTokenDataAsync(string state);
        Task DeleteTokenDataAsync(string state);
        
        // Redis 기반 세션 관리 (확장성 개선)
        Task StoreSessionAsync(string sessionId, object sessionData, TimeSpan? expiry = null);
        Task<object?> GetSessionAsync(string sessionId);
        Task DeleteSessionAsync(string sessionId);
        Task<bool> ExtendSessionAsync(string sessionId, TimeSpan expiry);
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
