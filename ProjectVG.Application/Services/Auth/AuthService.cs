using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.User;
using ProjectVG.Application.Services.Users;
using ProjectVG.Application.Services.Token;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Common.Exceptions;
using ProjectVG.Common.Constants;
using ProjectVG.Domain.Entities.Users;

namespace ProjectVG.Application.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ITokenManagementService _tokenManagementService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserService userService, 
            ITokenService tokenService,
            ITokenManagementService tokenManagementService,
            ILogger<AuthService> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _tokenManagementService = tokenManagementService;
            _logger = logger;
        }

        public async Task<AuthResult> LoginWithOAuthAsync(string provider, string providerUserId)
        {
            // OAuth 프로바이더별 사용자 처리
            Guid userId;
            UserDto user;

            if (provider == "guest")
            {
                if (string.IsNullOrEmpty(providerUserId))
                {
                    throw new ValidationException(ErrorCode.GUEST_ID_INVALID);
                }

                // 기존 게스트 사용자가 있는지 확인
                user = await _userService.TryGetByProviderAsync("guest", providerUserId);
                
                if (user == null)
                {
                    // 새로운 게스트 사용자 생성
                    string uuid = GenerateGuestUuid(providerUserId);
                    var createCommand = new UserCreateCommand(
                        Username: $"guest_{uuid}",
                        Email: $"guest@guest{uuid}.local",
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
            // 실제 OAuth 프로바이더인 경우 (Google, Apple 등)
            else if (provider == "google" || provider == "apple")
            {
                if (string.IsNullOrEmpty(providerUserId))
                {
                    throw new ValidationException(ErrorCode.PROVIDER_USER_ID_INVALID);
                }

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
                throw new ValidationException(ErrorCode.OAUTH2_PROVIDER_NOT_SUPPORTED);
            }

            // OAuth2 사용자인 경우 Provider 정보를 포함하여 사용자 생성 (test와 guest는 이미 처리됨)
            if (provider != "test" && provider != "guest")
            {
                user = user with { Provider = provider, ProviderId = providerUserId };
            }

            // 첫 로그인 토큰 지급 시도
            var tokenGranted = await _tokenManagementService.GrantInitialTokensAsync(user.Id);
            if (tokenGranted)
            {
                _logger.LogInformation("Initial tokens (5000) granted successfully to user {UserId}", user.Id);
            }
            else
            {
                _logger.LogInformation("Initial tokens already granted or grant failed for user {UserId}", user.Id);
            }

            var tokens = await _tokenService.GenerateTokensAsync(user.Id);
            
            _logger.LogInformation("Users {UserId} logged in with OAuth provider: {Provider}", user.Id, provider);
            
            return new AuthResult
            {
                Tokens = tokens,
                User = user
            };
        }

        public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new ValidationException(ErrorCode.TOKEN_MISSING, "리프레시 토큰이 필요합니다");
            }

            var tokens = await _tokenService.RefreshAccessTokenAsync(refreshToken);
            if (tokens == null)
            {
                throw new ValidationException(ErrorCode.TOKEN_REFRESH_FAILED, "유효하지 않거나 만료된 리프레시 토큰입니다");
            }

            var userId = await _tokenService.GetUserIdFromTokenAsync(refreshToken);
            var user = userId.HasValue ? await _userService.TryGetByIdAsync(userId.Value) : null;

            return new AuthResult
            {
                Tokens = tokens,
                User = user
            };
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new ValidationException(ErrorCode.TOKEN_MISSING, "리프레시 토큰이 필요합니다");
            }

            var revoked = await _tokenService.RevokeRefreshTokenAsync(refreshToken);
            if (revoked)
            {
                var userId = await _tokenService.GetUserIdFromTokenAsync(refreshToken);
                _logger.LogInformation("Users {UserId} logged out successfully", userId);
            }
            return revoked;
        }

        private static string GenerateGuestUuid(string providerUserId)
        {
            // SHA256 해시를 사용하여 일관된 UUID 생성
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(providerUserId));
            var hashString = Convert.ToHexString(hash);
            return hashString.Substring(0, Math.Min(hashString.Length, 16)).ToLowerInvariant();
        }
    }
}
