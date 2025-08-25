using System.Text.Json.Serialization;

namespace ProjectVG.Application.Models.Auth
{
    public class TokenExchangeRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
        public string GrantType { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    public class TokenResponse
    {
        public bool Success { get; set; }
        public Tokens? Tokens { get; set; }
        public UserData? User { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class Tokens
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = string.Empty;
    }

    public class UserData
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }

    public class OAuth2AuthRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string ClientRedirectUri { get; set; } = string.Empty; // 클라이언트가 최종적으로 리다이렉트받을 URL
        public string State { get; set; } = string.Empty;
        public string CodeChallenge { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
        public string CodeChallengeMethod { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class OAuth2TokenData
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string UID { get; set; } = string.Empty;
    }

    public class OAuth2CallbackResult
    {
        public bool Success { get; set; }
        public string? RedirectUrl { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
