using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Application.Services.Auth;

public interface IOAuth2Service
{
    Task<string> GenerateAuthorizationCodeAsync(
        string clientId, 
        Guid userId, 
        string codeChallenge, 
        string codeChallengeMethod, 
        string? redirectUri = null, 
        string? scope = null,
        string? ipAddress = null,
        string? userAgent = null);

    Task<AuthorizationCode?> ValidateAuthorizationCodeAsync(string code);

    Task<AuthorizationCode?> ExchangeCodeAsync(string code, string clientId, string codeVerifier, string? redirectUri = null);

    Task<bool> ValidateClientAsync(string clientId);

    Task<bool> ValidateRedirectUriAsync(string clientId, string? redirectUri);

    Task<bool> ValidatePkceAsync(string codeChallenge, string codeChallengeMethod, string codeVerifier);

    Task RevokeCodeAsync(string code);
}
