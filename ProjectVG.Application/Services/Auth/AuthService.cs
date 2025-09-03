using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Auth;
using ProjectVG.Application.Models.User;
using ProjectVG.Application.Services.Credit;
using ProjectVG.Application.Services.Users;
using ProjectVG.Common.Constants;
using ProjectVG.Common.Exceptions;
using ProjectVG.Domain.Entities.Users;
using ProjectVG.Infrastructure.Auth;
using System;

namespace ProjectVG.Application.Services.Auth
{
    public class AuthService : IUserAuthService
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

        public async Task<AuthResult> SignInWithOAuthAsync(string provider, string providerUserId)
        {
            return provider switch {
                "guest" => await GuestLoginAsync(providerUserId),
                "google" or "apple" => await OAuth2LoginAsync(provider, providerUserId),
                _ => throw new ValidationException(ErrorCode.OAUTH2_PROVIDER_NOT_SUPPORTED)
            };
        }

        private async Task<AuthResult> GuestLoginAsync(string guestId)
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
            else {
                _logger.LogDebug("기존 게스트 사용자 로그인: UserId={UserId}, GuestId={GuestId}", user.Id, guestId);
            }

            return await FinalizeLoginAsync(user, "guest");
        }

        private async Task<AuthResult> OAuth2LoginAsync(string provider, string providerUserId)
        {
            if (string.IsNullOrEmpty(providerUserId)) {
                throw new ValidationException(ErrorCode.PROVIDER_USER_ID_INVALID);
            }

            var user = await _userService.TryGetByProviderAsync(provider, providerUserId);

            if (user == null) {
                string uuid = GenerateGuestUuid(providerUserId);
                var createCommand = new UserCreateCommand(
                    Username: $"임시 유저 이름",
                    Email: $"guest@guest{uuid}.local",
                    ProviderId: providerUserId,
                    Provider: provider
                );


                user = new UserDto {
                    

                    Id = Guid.NewGuid(),
                    Username = $"{provider}_user_{providerUserId}",
                    Email = $"{providerUserId}@{provider}.oauth",
                    Status = AccountStatus.Active,
                    Provider = provider,
                    ProviderId = providerUserId
                };
            }

            _logger.LogInformation("새 OAuth 사용자 생성됨: UserId={UserId}, Provider={Provider}, ProviderId={ProviderId}",
                user.Id, provider, providerUserId);

            return await FinalizeLoginAsync(user, provider);
        }

        private async Task<AuthResult> FinalizeLoginAsync(UserDto user, string provider)
        {
            // 초기 크레딧 지급
            var tokenGranted = await _tokenManagementService.GrantInitialCreditsAsync(user.Id);
            if (tokenGranted) {
                _logger.LogInformation("사용자 {UserId}에게 최초 크레딧 지급 완료", user.Id);
            }
            else {
                _logger.LogDebug("사용자 {UserId}는 이미 크레딧이 지급되었거나 지급 실패", user.Id);
            }

            // 최종 JWT 토큰 발급
            var tokens = await _tokenService.GenerateTokensAsync(user.Id);

            _logger.LogDebug("사용자 {UserId} 로그인 완료 (Provider={Provider})", user.Id, provider);

            return new AuthResult {
                Tokens = tokens,
                User = user
            };
        }

        public async Task<AuthResult> RefreshAccessTokenAsync(string? refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken)) {
                throw new ValidationException(ErrorCode.TOKEN_MISSING, "리프레시 토큰이 필요합니다");
            }

            var tokens = await _tokenService.RefreshAccessTokenAsync(refreshToken);
            if (tokens == null) {
                throw new ValidationException(ErrorCode.TOKEN_REFRESH_FAILED, "유효하지 않거나 만료된 리프레시 토큰입니다");
            }

            var userId = await _tokenService.GetUserIdFromTokenAsync(refreshToken);
            var user = userId.HasValue ? await _userService.TryGetByIdAsync(userId.Value) : null;

            return new AuthResult {
                Tokens = tokens,
                User = user
            };
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken)) {
                throw new ValidationException(ErrorCode.TOKEN_MISSING, "리프레시 토큰이 필요합니다");
            }

            var revoked = await _tokenService.RevokeRefreshTokenAsync(refreshToken);
            if (revoked) {
                var userId = await _tokenService.GetUserIdFromTokenAsync(refreshToken);
                _logger.LogInformation("사용자 {UserId} 로그아웃 성공", userId);
            }
            else {
                _logger.LogWarning("리프레시 토큰 만료 또는 무효화 실패: {RefreshToken}", refreshToken);
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
