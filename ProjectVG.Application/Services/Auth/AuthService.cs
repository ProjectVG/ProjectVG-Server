using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.User;
using ProjectVG.Application.Services.Users;
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
        private readonly ILogger<AuthService> _logger;

        /// <summary>
        /// AuthService를 생성합니다. 필요한 서비스(IUserService, ITokenService)와 로거를 주입받아 내부 필드에 설정합니다.
        /// </summary>
        public AuthService(IUserService userService, ITokenService tokenService, ILogger<AuthService> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _logger = logger;
        }

        /// <summary>
        /// 지정된 OAuth 프로바이더로 사용자 인증(또는 게스트 흐름)을 수행하고 토큰 및 사용자 정보를 반환합니다.
        /// </summary>
        /// <param name="provider">인증 프로바이더 식별자. 지원 값: "guest", "google", "apple".</param>
        /// <param name="providerUserId">프로바이더에서 제공한 사용자 식별자(또는 게스트 ID).</param>
        /// <returns>생성되거나 조회된 사용자 정보와 액세스/리프레시 토큰을 포함한 AuthResult.</returns>
        /// <exception cref="ValidationException">다음 경우에 발생합니다:
        /// <list type="bullet">
        /// <item>provider가 "guest"이고 providerUserId가 비어있을 경우: ErrorCode.GUEST_ID_INVALID</item>
        /// <item>provider가 "google" 또는 "apple"이고 providerUserId가 비어있을 경우: ErrorCode.PROVIDER_USER_ID_INVALID</item>
        /// <item>지원되지 않는 프로바이더가 지정된 경우: ErrorCode.OAUTH2_PROVIDER_NOT_SUPPORTED</item>
        /// </list>
        /// </exception>
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
                    string uuid = providerUserId.Substring(0, Math.Min(providerUserId.Length, 20));
                    // 새로운 게스트 사용자 생성
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
                user.Provider = provider;
                user.ProviderId = providerUserId;
            }

            var tokens = await _tokenService.GenerateTokensAsync(user.Id);
            
            _logger.LogInformation("Users {UserId} logged in with OAuth provider: {Provider}", user.Id, provider);
            
            return new AuthResult
            {
                Tokens = tokens,
                User = user
            };
        }

        /// <summary>
        /// 리프레시 토큰으로 액세스 토큰을 재발급하고 새 토큰과 연관된 사용자 정보를 반환합니다.
        /// </summary>
        /// <param name="refreshToken">재발급에 사용할 리프레시 토큰(빈 값일 수 없음).</param>
        /// <returns>재발급된 토큰(`Tokens`)과 해당 토큰에 연결된 사용자(`User`)를 포함하는 <see cref="AuthResult"/>.</returns>
        /// <exception cref="ValidationException">
        /// <list type="bullet">
        /// <item>토큰이 null 또는 빈 문자열인 경우: <see cref="ErrorCode.TOKEN_MISSING"/>.</item>
        /// <item>리프레시 토큰으로부터 새로운 액세스 토큰을 얻지 못한 경우(유효하지 않거나 만료된 토큰): <see cref="ErrorCode.TOKEN_REFRESH_FAILED"/>.</item>
        /// </list>
        /// </exception>
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

        /// <summary>
        /// 주어진 리프레시 토큰을 폐기하여 사용자 로그아웃을 처리합니다.
        /// </summary>
        /// <param name="refreshToken">폐기할 리프레시 토큰(빈 문자열 또는 null일 수 없습니다).</param>
        /// <returns>토큰 폐기가 성공하면 true, 그렇지 않으면 false를 반환합니다.</returns>
        /// <exception cref="ValidationException">refreshToken이 null 또는 빈 문자열인 경우(ErrorCode.TOKEN_MISSING).</exception>
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
    }
}
