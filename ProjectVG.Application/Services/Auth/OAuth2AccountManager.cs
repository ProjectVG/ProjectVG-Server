using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Auth;
using ProjectVG.Application.Models.User;
using ProjectVG.Application.Services.Users;
using ProjectVG.Common.Constants;
using ProjectVG.Common.Exceptions;

namespace ProjectVG.Application.Services.Auth
{
    public class OAuth2AccountManager : IOAuth2AccountManager
    {
        private readonly IUserService _userService;
        private readonly IOAuth2AuthService _oAuth2AuthService;
        private readonly ILogger<OAuth2AccountManager> _logger;

        public OAuth2AccountManager(
            IUserService userService,
            IOAuth2AuthService oAuth2AuthService,
            ILogger<OAuth2AccountManager> logger)
        {
            _userService = userService;
            _oAuth2AuthService = oAuth2AuthService;
            _logger = logger;
        }

        public async Task<AuthResult> ProcessOAuth2LoginAsync(string provider, OAuth2UserInfo userInfo)
        {
            if (string.IsNullOrEmpty(userInfo.Id)) {
                throw new ValidationException(ErrorCode.PROVIDER_USER_ID_INVALID);
            }

            var existingUser = await _userService.TryGetByProviderAsync(provider, userInfo.Id);

            if (existingUser != null) {
                _logger.LogDebug("기존 OAuth 계정 로그인: UserId={UserId}, Provider={Provider}", existingUser.Id, provider);
                return await _oAuth2AuthService.OAuth2LoginAsync(provider, userInfo.Id, existingUser.Email);
            }

            _logger.LogInformation("새 OAuth 계정 생성: ProviderId={ProviderId}, Provider={Provider}, Email={Email}", 
                userInfo.Id, provider, userInfo.Email);
            
            return await _oAuth2AuthService.OAuth2LoginAsync(provider, userInfo.Id, userInfo.Email);
        }
    }
}