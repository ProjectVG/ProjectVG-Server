using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectVG.Application.Models.Auth;
using ProjectVG.Infrastructure.Cache;
using System.Security.Cryptography;
using System.Text;

namespace ProjectVG.Application.Services.Auth;

public class OAuth2Service : IOAuth2Service
{
    private readonly IRedisCacheService _cache;
    private readonly ILogger<OAuth2Service> _logger;
    private readonly OAuth2Settings _oauth2Settings;

    private const string AUTHORIZATION_CODE_PREFIX = "oauth2:code";
    private const int AUTHORIZATION_CODE_LIFETIME_MINUTES = 10;

    public OAuth2Service(
        IRedisCacheService cache,
        ILogger<OAuth2Service> logger,
        IOptions<OAuth2Settings> oauth2Settings)
    {
        _cache = cache;
        _logger = logger;
        _oauth2Settings = oauth2Settings.Value;
    }

    public async Task<string> GenerateAuthorizationCodeAsync(
        string clientId, 
        Guid userId, 
        string codeChallenge, 
        string codeChallengeMethod, 
        string? redirectUri = null, 
        string? scope = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var code = GenerateSecureCode();
        var authCode = new AuthorizationCode
        {
            Code = code,
            ClientId = clientId,
            UserId = userId,
            RedirectUri = redirectUri,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod,
            Scope = scope,
            ExpiresAt = DateTime.UtcNow.AddMinutes(AUTHORIZATION_CODE_LIFETIME_MINUTES),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        var key = GetAuthorizationCodeKey(code);
        var serializedCode = System.Text.Json.JsonSerializer.Serialize(authCode);
        
        await _cache.SetStringAsync(key, serializedCode, TimeSpan.FromMinutes(AUTHORIZATION_CODE_LIFETIME_MINUTES));

        _logger.LogInformation("Generated authorization code for client {ClientId}, user {UserId}", 
            clientId, userId);

        return code;
    }

    public async Task<AuthorizationCode?> ValidateAuthorizationCodeAsync(string code)
    {
        var key = GetAuthorizationCodeKey(code);
        var serializedCode = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(serializedCode))
        {
            return null;
        }

        try
        {
            var authCode = System.Text.Json.JsonSerializer.Deserialize<AuthorizationCode>(serializedCode);
            return authCode?.IsValid == true ? authCode : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize authorization code");
            return null;
        }
    }

    public async Task<AuthorizationCode?> ExchangeCodeAsync(string code, string clientId, string codeVerifier, string? redirectUri = null)
    {
        var authCode = await ValidateAuthorizationCodeAsync(code);
        
        if (authCode == null)
        {
            _logger.LogWarning("Invalid or expired authorization code attempted");
            return null;
        }

        // 클라이언트 ID 검증
        if (authCode.ClientId != clientId)
        {
            _logger.LogWarning("Client ID mismatch for authorization code");
            return null;
        }

        // Redirect URI 검증
        if (authCode.RedirectUri != redirectUri)
        {
            _logger.LogWarning("Redirect URI mismatch for authorization code");
            return null;
        }

        // PKCE 검증
        if (!await ValidatePkceAsync(authCode.CodeChallenge, authCode.CodeChallengeMethod, codeVerifier))
        {
            _logger.LogWarning("PKCE validation failed for authorization code");
            return null;
        }

        // 코드 사용 처리
        await RevokeCodeAsync(code);

        _logger.LogInformation("Successfully exchanged authorization code for client {ClientId}, user {UserId}", 
            clientId, authCode.UserId);

        return authCode;
    }

    public async Task<bool> ValidateClientAsync(string clientId)
    {
        var client = _oauth2Settings.Clients?.FirstOrDefault(c => c.ClientId == clientId);
        return await Task.FromResult(client != null);
    }

    public async Task<bool> ValidateRedirectUriAsync(string clientId, string? redirectUri)
    {
        if (string.IsNullOrEmpty(redirectUri))
        {
            return true; // Optional redirect URI
        }

        var isAllowed = _oauth2Settings.RedirectUris?.Contains(redirectUri) == true;
        return await Task.FromResult(isAllowed);
    }

    public async Task<bool> ValidatePkceAsync(string codeChallenge, string codeChallengeMethod, string codeVerifier)
    {
        if (codeChallengeMethod != "S256")
        {
            _logger.LogWarning("Unsupported code challenge method: {Method}", codeChallengeMethod);
            return false;
        }

        try
        {
            var hashedVerifier = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier)))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            var isValid = hashedVerifier == codeChallenge;
            
            if (!isValid)
            {
                _logger.LogWarning("PKCE verification failed");
            }

            return await Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PKCE");
            return false;
        }
    }

    public async Task RevokeCodeAsync(string code)
    {
        var key = GetAuthorizationCodeKey(code);
        await _cache.DeleteAsync(key);
        
        _logger.LogInformation("Revoked authorization code");
    }

    private static string GenerateSecureCode()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string GetAuthorizationCodeKey(string code)
    {
        return $"{AUTHORIZATION_CODE_PREFIX}:{code}";
    }
}
