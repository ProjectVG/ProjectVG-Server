using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.User;
using ProjectVG.Application.Services.User;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Common.Models;

namespace ProjectVG.Application.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserService userService, ITokenService tokenService, ILogger<AuthService> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<AuthResult> LoginWithOAuthAsync(string provider, string accessToken)
        {
            try
            {
                // TODO: 실제 OAuth 검증 로직 구현
                // 임시로 테스트용 사용자 생성 (테스트 시 accessToken이 UserId로 전달됨)
                Guid userId;
                if (provider == "test" && Guid.TryParse(accessToken, out userId))
                {
                    var testUser = new UserDto
                    {
                        Id = userId,
                        Username = $"test_user_{userId}",
                        Name = $"Test User {userId}",
                        Email = $"test{userId}@example.com",
                        Provider = provider,
                        ProviderId = accessToken,
                        IsActive = true
                    };

                    var tokens = await _tokenService.GenerateTokensAsync(testUser.Id);
                    
                    _logger.LogInformation("User {UserId} logged in with OAuth provider: {Provider}", testUser.Id, provider);
                    
                    return new AuthResult
                    {
                        IsSuccess = true,
                        Tokens = tokens,
                        User = testUser
                    };
                }
                else
                {
                    return new AuthResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid test user ID format"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OAuth login failed for provider: {Provider}", provider);
                return new AuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = "OAuth authentication failed due to internal error"
                };
            }
        }

        public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var tokens = await _tokenService.RefreshAccessTokenAsync(refreshToken);
                if (tokens == null)
                {
                    return new AuthResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Invalid or expired refresh token"
                    };
                }

                var userId = await _tokenService.GetUserIdFromTokenAsync(refreshToken);
                var user = userId.HasValue ? await _userService.GetUserByIdAsync(userId.Value) : null;

                return new AuthResult
                {
                    IsSuccess = true,
                    Tokens = tokens,
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return new AuthResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Token refresh failed due to internal error"
                };
            }
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            try
            {
                var revoked = await _tokenService.RevokeRefreshTokenAsync(refreshToken);
                if (revoked)
                {
                    var userId = await _tokenService.GetUserIdFromTokenAsync(refreshToken);
                    _logger.LogInformation("User {UserId} logged out successfully", userId);
                }
                return revoked;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                return false;
            }
        }

        public async Task<bool> ValidateAccessTokenAsync(string accessToken)
        {
            try
            {
                _logger.LogInformation("=== ValidateAccessTokenAsync 디버그 ===");
                
                var userId = await _tokenService.GetUserIdFromTokenAsync(accessToken);
                _logger.LogInformation("추출된 UserId: {UserId}", userId);
                
                if (!userId.HasValue)
                {
                    _logger.LogWarning("UserId가 null이므로 검증 실패");
                    return false;
                }

                _logger.LogInformation("JWT 토큰 검증 성공 - UserId: {UserId}", userId.Value);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Access token validation failed");
                return false;
            }
        }

        public async Task<Guid?> GetCurrentUserIdAsync(string accessToken)
        {
            try
            {
                return await _tokenService.GetUserIdFromTokenAsync(accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user ID from access token");
                return null;
            }
        }
    }
}
