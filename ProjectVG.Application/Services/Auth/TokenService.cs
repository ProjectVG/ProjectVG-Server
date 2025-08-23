using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectVG.Application.Models.Auth;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Infrastructure.Auth.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ProjectVG.Application.Services.Auth;

public class TokenService : ITokenService
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IKeyStore _keyStore;
    private readonly ILogger<TokenService> _logger;
    private readonly JwtSettings _jwtSettings;

    public TokenService(
        IRefreshTokenService refreshTokenService,
        IKeyStore keyStore,
        ILogger<TokenService> logger,
        IOptions<JwtSettings> jwtSettings)
    {
        _refreshTokenService = refreshTokenService;
        _keyStore = keyStore;
        _logger = logger;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<TokenResponse> GenerateTokensAsync(Guid userId, string clientId, string? deviceId = null, string? ipAddress = null, string? userAgent = null)
    {
        // Access Token 생성
        var accessToken = await GenerateAccessTokenAsync(userId);

        // Refresh Token 생성
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenLifetimeDays);
        var refreshToken = await _refreshTokenService.CreateAsync(userId, clientId, deviceId, refreshTokenExpiry, ipAddress, userAgent);

        _logger.LogInformation("Generated tokens for user {UserId}, client {ClientId}", userId, clientId);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.TokenHash, // 실제 토큰 값 반환
            ExpiresIn = _jwtSettings.AccessTokenLifetimeMinutes * 60,
            TokenType = "Bearer"
        };
    }

    public async Task<Models.Auth.TokenValidationResult> ValidateAccessTokenAsync(string token)
    {
        try
        {
            var signingKeys = await _keyStore.GetValidationKeysAsync();
            var securityKeys = signingKeys.Cast<SecurityKey>();
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKeys = securityKeys,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            
            var userIdClaim = principal.FindFirst("sub")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return new Models.Auth.TokenValidationResult { IsValid = false, ErrorMessage = "Invalid user ID in token" };
            }

            return new Models.Auth.TokenValidationResult 
            { 
                IsValid = true, 
                UserId = userId,
                Claims = principal.Claims.ToList()
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new Models.Auth.TokenValidationResult { IsValid = false, ErrorMessage = "Token has expired" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return new Models.Auth.TokenValidationResult { IsValid = false, ErrorMessage = "Token validation failed" };
        }
    }

    public async Task<string> GenerateAccessTokenAsync(Guid userId, string? scope = null)
    {
        var signingKey = await _keyStore.GetCurrentSigningKeyAsync();
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrEmpty(scope))
        {
            claims.Add(new Claim("scope", scope));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenLifetimeMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        return tokenHandler.WriteToken(token);
    }
}
