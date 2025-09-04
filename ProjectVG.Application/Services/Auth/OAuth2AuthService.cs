using ProjectVG.Application.Models.User;
using ProjectVG.Application.Services.Credit;
using ProjectVG.Application.Services.Users;
using ProjectVG.Infrastructure.Auth;

namespace ProjectVG.Application.Services.Auth
{
    public class OAuth2AuthService : IOAuth2AuthService
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ICreditManagementService _tokenManagementService;
        private readonly ILogger<OAuth2AuthService> _logger;

        public OAuth2AuthService(
            IUserService userService,
            ITokenService tokenService,
            ICreditManagementService tokenManagementService,
            ILogger<OAuth2AuthService> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _tokenManagementService = tokenManagementService;
            _logger = logger;
        }

        public async Task<AuthResult> OAuth2LoginAsync(string provider, string providerUserId, string email)
        {
            if (string.IsNullOrEmpty(providerUserId)) {
                throw new ValidationException(ErrorCode.PROVIDER_USER_ID_INVALID);
            }

            var user = await _userService.TryGetByProviderAsync(provider, providerUserId);

            if (user == null) {
                user = await CreateOAuth2UserAsync(provider, providerUserId, email);
                _logger.LogInformation("새 OAuth 사용자 생성: UserId={UserId}, Provider={Provider}", user.Id, provider);
            }
            else {
                _logger.LogDebug("기존 OAuth 사용자 로그인: UserId={UserId}, Provider={Provider}", user.Id, provider);
            }

            return await FinalizeLoginAsync(user, provider);
        }

        private async Task<UserDto> CreateOAuth2UserAsync(string provider, string providerUserId, string email)
        {
            var username = string.IsNullOrEmpty(email) 
                ? $"{provider}_{GenerateUserSuffix(providerUserId)}"
                : email.Split('@')[0];

            var userEmail = string.IsNullOrEmpty(email)
                ? $"{providerUserId}@{provider}.oauth"
                : email;

            var createCommand = new UserCreateCommand(
                Username: username,
                Email: userEmail,
                ProviderId: providerUserId,
                Provider: provider
            );

            return await _userService.CreateUserAsync(createCommand);
        }

        private async Task<AuthResult> FinalizeLoginAsync(UserDto user, string provider)
        {
            var tokenGranted = await _tokenManagementService.GrantInitialCreditsAsync(user.Id);
            if (tokenGranted) {
                _logger.LogInformation("사용자 {UserId} 최초 크레딧 지급 완료", user.Id);
            }

            var tokens = await _tokenService.GenerateTokensAsync(user.Id);

            _logger.LogDebug("사용자 {UserId} OAuth 로그인 완료 (Provider={Provider})", user.Id, provider);

            return new AuthResult {
                Tokens = tokens,
                User = user
            };
        }

        private static string GenerateUserSuffix(string providerUserId)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(providerUserId));
            var hashString = Convert.ToHexString(hash);
            return hashString.Substring(0, Math.Min(hashString.Length, 8)).ToLowerInvariant();
        }
    }
}