using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Application.Services.Auth
{
    public interface IOAuth2CodeValidator
    {
        Task<TokenResponse> ValidateAndExchangeCodeAsync(string code, string clientId, string redirectUri, string codeVerifier = "");
    }
}