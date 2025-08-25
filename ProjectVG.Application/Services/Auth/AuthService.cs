using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.User;
using ProjectVG.Application.Services.Users;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Common.Models;
using ProjectVG.Domain.Entities.Users;

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

        public async Task<AuthResult> LoginWithOAuthAsync(string provider, string providerUserId)
        {
            try
            {
                // OAuth 프로바이더별 사용자 처리
                Guid userId;
                UserDto user;

                // 테스트 프로바이더인 경우
                if (provider == "test")
                {
                    if (!Guid.TryParse(providerUserId, out userId))
                    {
                        return new AuthResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "Invalid test user ID format"
                        };
                    }
                    
                    user = new UserDto
                    {
                        Id = userId,
                        Username = $"test_user_{userId}",
                        Email = $"test{userId}@example.com",
                        Status = AccountStatus.Active,
                        Provider = provider,
                        ProviderId = providerUserId
                    };
                }
                // 게스트 로그인인 경우
                else if (provider == "guest")
                {
                    // 기존 게스트 사용자가 있는지 확인
                    user = await _userService.TryGetByProviderAsync("guest", providerUserId);
                    
                    if (user == null)
                    {
                        // 새로운 게스트 사용자 생성
                        var createCommand = new UserCreateCommand(
                            Username: $"guest_{providerUserId}",
                            Email: $"guest_{providerUserId}@guest.local",
                            ProviderId: providerUserId,
                            Provider: "guest"
                        );
                        
                        user = await _userService.CreateUserAsync(createCommand);
                        _logger.LogInformation("New guest user created: {UserId} with GuestId: {GuestId}", user.Id, providerUserId);
                    }
                    else
                    {
                        _logger.LogInformation("Existing guest user logged in: {UserId} with GuestId: {GuestId}", user.Id, providerUserId);
                    }
                }
                // 실제 OAuth 프로바이더인 경우 (Google, GitHub 등)
                else if (provider == "google" || provider == "github" || provider == "microsoft")
                {
                    // 새로운 사용자 ID 생성
                    userId = Guid.NewGuid();
                    
                    user = new UserDto
                    {
                        Id = userId,
                        Username = $"{provider}_user_{providerUserId}",
                        Email = $"{providerUserId}@{provider}.oauth",
                        Status = AccountStatus.Active
                    };

                    _logger.LogInformation("New OAuth user created: {UserId} from {Provider} with ProviderId: {ProviderId}", 
                        userId, provider, providerUserId);
                }
                else
                {
                    return new AuthResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Unsupported OAuth provider: {provider}"
                    };
                }

                // OAuth2 사용자인 경우 Provider 정보를 포함하여 사용자 생성 (test와 guest는 이미 처리됨)
                if (provider != "test" && provider != "guest")
                {
                    user.Provider = provider;
                    user.ProviderId = providerUserId;
                }

                var tokens = await _tokenService.GenerateTokensAsync(user.Id);
                
                _logger.LogInformation("Users {UserId} logged in with OAuth provider: {Provider}", user.Id, provider);
                
                return new AuthResult
                {
                    IsSuccess = true,
                    Tokens = tokens,
                    User = user
                };
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
                var user = userId.HasValue ? await _userService.TryGetByIdAsync(userId.Value) : null;

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
                    _logger.LogInformation("Users {UserId} logged out successfully", userId);
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
