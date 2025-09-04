using ProjectVG.Application.Models.User;
using ProjectVG.Application.Services.Credit;
using ProjectVG.Application.Services.Users;
using ProjectVG.Infrastructure.Auth;

namespace ProjectVG.Application.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ICreditManagementService _tokenManagementService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserService userService,
            ITokenService tokenService,
            ICreditManagementService tokenManagementService,
            ILogger<AuthService> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _tokenManagementService = tokenManagementService;
            _logger = logger;
        }

        public async Task<AuthResult> GuestLoginAsync(string guestId)
        {
            if (string.IsNullOrEmpty(guestId)) {
                throw new ValidationException(ErrorCode.GUEST_ID_INVALID);
            }

            var user = await _userService.TryGetByProviderAsync("guest", guestId);

            if (user == null) {
                string uuid = GenerateGuestUuid(guestId);
                var createCommand = new UserCreateCommand(
                    Username: $"guest_{uuid}",
                    Email: $"guest@guest{uuid}.local",
                    ProviderId: guestId,
                    Provider: "guest"
                );

                user = await _userService.CreateUserAsync(createCommand);
                _logger.LogInformation("새 게스트 사용자 생성됨: UserId={UserId}, GuestId={GuestId}", user.Id, guestId);
            }

            return await FinalizeLoginAsync(user, "guest");
        }

        private async Task<AuthResult> FinalizeLoginAsync(UserDto user, string provider)
        {
            // 초기 크레딧 지급
            var tokenGranted = await _tokenManagementService.GrantInitialCreditsAsync(user.Id);
            if (tokenGranted) {
                _logger.LogInformation("사용자 {UserId}에게 최초 크레딧 지급 완료", user.Id);
            }

            // 최종 JWT 토큰 발급
            var tokens = await _tokenService.GenerateTokensAsync(user.Id);

            return new AuthResult {
                Tokens = tokens,
                User = user
            };
        }

        public async Task<AuthResult> RefreshAccessTokenAsync(string? refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken)) {
                throw new ValidationException(ErrorCode.TOKEN_MISSING);
            }

            var tokens = await _tokenService.RefreshAccessTokenAsync(refreshToken);
            if (tokens == null) {
                throw new ValidationException(ErrorCode.TOKEN_REFRESH_FAILED);
            }

            var userId = await _tokenService.GetUserIdFromTokenAsync(refreshToken);
            var user = userId.HasValue ? await _userService.TryGetByIdAsync(userId.Value) : null;

            return new AuthResult {
                Tokens = tokens,
                User = user
            };
        }

        public async Task<bool> LogoutAsync(string? refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken)) {
                throw new ValidationException(ErrorCode.TOKEN_MISSING);
            }

            var revoked = await _tokenService.RevokeRefreshTokenAsync(refreshToken);
            if (revoked) {
                var userId = await _tokenService.GetUserIdFromTokenAsync(refreshToken);
            }
            return revoked;
        }
        private static string GenerateGuestUuid(string providerUserId)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(providerUserId));
            var hashString = Convert.ToHexString(hash);
            return hashString.Substring(0, Math.Min(hashString.Length, 16)).ToLowerInvariant();
        }

    }
}
