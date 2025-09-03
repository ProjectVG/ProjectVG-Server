using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using ProjectVG.Common.Configuration;
using ProjectVG.Application.Models.Auth;
using ProjectVG.Common.Exceptions;
using ProjectVG.Common.Constants;

namespace ProjectVG.Application.Services.Auth
{
    /// <summary>
    /// OAuth2 인증 플로우 관리 서비스
    /// 팩토리 패턴을 사용하여 각 OAuth2 제공자별로 독립적인 처리
    /// </summary>
    public class OAuth2Service : IOAuth2Service
    {
        private readonly ILogger<OAuth2Service> _logger;
        private readonly OAuth2ProviderSettings _settings;
        private readonly IOAuth2CodeValidator _codeValidator;
        private readonly IOAuth2UserService _userService;
        private readonly IOAuth2AccountManager _accountManager;
        private readonly IOAuth2ProviderFactory _providerFactory;
        private readonly IDistributedCache _cache;

        // Redis 키 접두사
        private const string OAuth2RequestPrefix = "oauth2:request:";
        private const string TokenDataPrefix = "oauth2:token:";

        // TTL 설정
        private static readonly TimeSpan OAuth2RequestTTL = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan TokenDataTTL = TimeSpan.FromMinutes(5);

        public OAuth2Service(
            ILogger<OAuth2Service> logger,
            IOptions<OAuth2ProviderSettings> settings,
            IOAuth2CodeValidator codeValidator,
            IOAuth2UserService userService,
            IOAuth2AccountManager accountManager,
            IOAuth2ProviderFactory providerFactory,
            IDistributedCache cache)
        {
            _logger = logger;
            _settings = settings.Value;
            _codeValidator = codeValidator;
            _userService = userService;
            _accountManager = accountManager;
            _providerFactory = providerFactory;
            _cache = cache;
        }

        public async Task<string> BuildAuthorizationUrlAsync(
            string providerName, string state,
            string codeChallenge, string codeChallengeMethod,
            string codeVerifier, string clientRedirectUri)
        {
            var provider = _providerFactory.GetProvider(providerName);

            if (!_settings.Providers.TryGetValue(providerName, out var providerSettings)
                || !providerSettings.Enabled) {
                throw new ValidationException(ErrorCode.OAUTH2_PROVIDER_NOT_CONFIGURED);
            }

            if (string.IsNullOrEmpty(providerSettings.ClientId)) {
                throw new ValidationException(ErrorCode.OAUTH2_CLIENT_ID_INVALID);
            }

            var authRequest = new OAuth2AuthRequest {
                ClientId = providerSettings.ClientId,
                RedirectUri = providerSettings.RedirectUri,
                ClientRedirectUri = clientRedirectUri,
                State = state,
                CodeChallenge = codeChallenge,
                CodeVerifier = codeVerifier,
                CodeChallengeMethod = codeChallengeMethod,
                CreatedAt = DateTime.UtcNow
            };

            await StoreOAuth2RequestAsync(state, authRequest);

            var authUrl = provider.BuildAuthorizationUrl(
                providerSettings.ClientId,
                providerSettings.RedirectUri,
                state,
                codeChallenge,
                codeChallengeMethod,
                provider.DefaultScopes);

            return authUrl;
        }

        public async Task<OAuth2CallbackResult> HandleOAuth2CallbackAsync(string code, string state)
        {
            var authRequest = await GetOAuth2RequestAsync(state);
            if (authRequest == null) {
                throw new ValidationException(ErrorCode.OAUTH2_REQUEST_NOT_FOUND);
            }

            var tokenResponse = await _codeValidator.ValidateAndExchangeCodeAsync(
                code,
                authRequest.ClientId,
                authRequest.RedirectUri,
                authRequest.CodeVerifier);

            if (!tokenResponse.Success) {
                throw new ValidationException(ErrorCode.OAUTH2_TOKEN_EXCHANGE_FAILED);
            }

            await DeleteOAuth2RequestAsync(state);

            var providerName = GetProviderNameFromClientId(authRequest.ClientId);
            var userInfo = await _userService.GetUserInfoAsync(tokenResponse.Tokens!.AccessToken, providerName);

            if (string.IsNullOrEmpty(userInfo.Id)) {
                throw new ValidationException(ErrorCode.OAUTH2_USER_INFO_FAILED);
            }

            var authResult = await _accountManager.ProcessOAuth2LoginAsync(providerName, userInfo);

            var tokenData = new OAuth2TokenData {
                AccessToken = authResult.Tokens!.AccessToken,
                RefreshToken = authResult.Tokens.RefreshToken,
                ExpiresIn = (int)(authResult.Tokens.AccessTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
                UID = authResult.User!.UID
            };

            await StoreTokenDataAsync(state, tokenData);

            var clientRedirectUrl = $"{authRequest.ClientRedirectUri}?" + $"success=true&" + $"state={Uri.EscapeDataString(state)}";

            return new OAuth2CallbackResult {
                Success = true,
                RedirectUrl = clientRedirectUrl
            };
        }


        public async Task<OAuth2AuthRequest> StoreOAuth2RequestAsync(string state, OAuth2AuthRequest request)
        {
            try {
                var key = OAuth2RequestPrefix + state;
                var json = JsonSerializer.Serialize(request);
                var options = new DistributedCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow = OAuth2RequestTTL
                };

                await _cache.SetStringAsync(key, json, options);
                _logger.LogDebug("OAuth2 요청 저장 완료: State={State}, TTL={TTL}분", state, OAuth2RequestTTL.TotalMinutes);
                return request;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "OAuth2 요청 저장 실패: State={State}", state);
                throw;
            }
        }

        public async Task<OAuth2AuthRequest?> GetOAuth2RequestAsync(string state)
        {
            try {
                var key = OAuth2RequestPrefix + state;
                var json = await _cache.GetStringAsync(key);

                if (!string.IsNullOrEmpty(json)) {
                    var request = JsonSerializer.Deserialize<OAuth2AuthRequest>(json);
                    _logger.LogDebug("OAuth2 요청 조회 성공: State={State}", state);
                    return request;
                }

                _logger.LogWarning("OAuth2 요청을 찾을 수 없음: State={State}", state);
                return null;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "OAuth2 요청 조회 실패: State={State}", state);
                return null;
            }
        }

        public async Task DeleteOAuth2RequestAsync(string state)
        {
            try {
                var key = OAuth2RequestPrefix + state;
                await _cache.RemoveAsync(key);
                _logger.LogDebug("OAuth2 요청 삭제 완료: State={State}", state);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "OAuth2 요청 삭제 실패: State={State}", state);
            }
        }

        public async Task StoreTokenDataAsync(string state, object tokenData)
        {
            try {
                var key = TokenDataPrefix + state;
                var json = JsonSerializer.Serialize(tokenData);
                var options = new DistributedCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow = TokenDataTTL
                };

                await _cache.SetStringAsync(key, json, options);
                _logger.LogDebug("토큰 데이터 저장 완료: State={State}, TTL={TTL}분", state, TokenDataTTL.TotalMinutes);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "토큰 데이터 저장 실패: State={State}", state);
                throw;
            }
        }

        public async Task DeleteTokenDataAsync(string state)
        {
            try {
                var key = TokenDataPrefix + state;
                await _cache.RemoveAsync(key);
                _logger.LogDebug("토큰 데이터 삭제 완료: State={State}", state);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "토큰 데이터 삭제 실패: State={State}", state);
            }
        }

        public async Task<OAuth2TokenData?> GetTokenDataAsync(string state)
        {
            try {
                var key = TokenDataPrefix + state;
                var json = await _cache.GetStringAsync(key);

                if (!string.IsNullOrEmpty(json)) {
                    var tokenData = JsonSerializer.Deserialize<OAuth2TokenData>(json);
                    _logger.LogDebug("토큰 데이터 조회 성공: State={State}", state);
                    return tokenData;
                }

                _logger.LogWarning("토큰 데이터를 찾을 수 없음: State={State}", state);
                return null;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "토큰 데이터 조회 실패: State={State}", state);
                return null;
            }
        }


        private string GetProviderNameFromClientId(string clientId)
        {
            foreach (var provider in _settings.Providers) {
                if (provider.Value.ClientId == clientId) {
                    return provider.Key;
                }
            }
            return "google";
        }
    }
}
