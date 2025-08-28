using System.Net.Http.Headers;
using System.Text.Json;
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
        private readonly HttpClient _httpClient;
        private readonly ILogger<OAuth2Service> _logger;
        private readonly OAuth2ProviderSettings _settings;
        private readonly IAuthService _authService;
        private readonly IOAuth2ProviderFactory _providerFactory;
        private readonly Dictionary<string, string> _oauth2Requests = new();
        private readonly Dictionary<string, string> _tokenData = new();

        /// <summary>
        /// OAuth2Service의 인스턴스를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 인증 흐름을 처리하기 위해 필요한 HTTP 클라이언트, 로거, 제공자 설정, 인증 서비스 및 제공자 팩토리를 주입받아 내부 필드를 초기화합니다.
        /// </remarks>
        public OAuth2Service(
            IHttpClientFactory httpClientFactory,
            ILogger<OAuth2Service> logger,
            IOptions<OAuth2ProviderSettings> settings,
            IAuthService authService,
            IOAuth2ProviderFactory providerFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _settings = settings.Value;
            _authService = authService;
            _providerFactory = providerFactory;
        }

        /// <summary>
        /// 특정 제공자로 OAuth2 인증 URL 생성
        /// </summary>
        /// <param name="providerName">제공자 이름 (google, apple)</param>
        /// <param name="state">상태값</param>
        /// <param name="codeChallenge">PKCE code challenge</param>
        /// <param name="codeChallengeMethod">PKCE challenge 방법</param>
        /// <param name="codeVerifier">PKCE code verifier</param>
        /// <param name="clientRedirectUri">클라이언트 리다이렉트 URI</param>
        /// <summary>
        /// 지정된 OAuth2 공급자에 대한 인증(authorization) URL을 생성합니다.
        /// </summary>
        /// <param name="providerName">사용할 OAuth2 공급자 식별자(예: "google").</param>
        /// <param name="state">클라이언트에서 전달한 상태값(state). 요청 식별 및 CSRF 방지에 사용됩니다.</param>
        /// <param name="codeChallenge">PKCE에서 사용되는 code_challenge 문자열(없으면 빈 문자열 가능).</param>
        /// <param name="codeChallengeMethod">PKCE 코드 챌린지 방식(e.g. "S256").</param>
        /// <param name="codeVerifier">PKCE에서 사용되는 code_verifier(서버에 저장됨, 필요 시 빈 문자열 가능).</param>
        /// <param name="clientRedirectUri">클라이언트 측 리디렉션 URI(인증 후 클라이언트로 돌아갈 주소, 내부 요청에 저장됨).</param>
        /// <returns>공급자별로 생성된 OAuth2 인증 URL 문자열.</returns>
        /// <exception cref="ValidationException">지정한 공급자가 구성되지 않았거나 비활성화된 경우, 또는 공급자의 클라이언트 ID가 유효하지 않은 경우 발생합니다.</exception>
        public async Task<string> BuildAuthorizationUrlAsync(string providerName, string state, string codeChallenge, string codeChallengeMethod, string codeVerifier, string clientRedirectUri)
        {
            var provider = _providerFactory.GetProvider(providerName);

            if (!_settings.Providers.TryGetValue(providerName, out var providerSettings) || !providerSettings.Enabled) {
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

        /// <summary>
        /// OAuth2 콜백을 처리하고 클라이언트로 리디렉션할 URL을 반환합니다.
        /// </summary>
        /// <remarks>
        /// 저장된 OAuth2 요청(state)을 조회하고, 받은 authorization code로 토큰을 교환한 뒤
        /// 공급자에서 사용자 정보를 조회하여 애플리케이션에 로그인합니다. 로그인 결과의 토큰 정보를
        /// 상태(state)에 연계하여 저장하고, 클라이언트 리디렉션용 URL을 생성해 반환합니다.
        /// </remarks>
        /// <param name="code">OAuth2 공급자가 콜백으로 전달한 authorization code.</param>
        /// <param name="state">초기에 생성되어 전달된 state 값 (요청 식별자).</param>
        /// <returns>
        /// OAuth2CallbackResult:
        /// - Success: 처리 성공 여부 (성공 시 true)
        /// - RedirectUrl: 클라이언트로 리디렉트할 URL (성공/상태 파라미터 포함)
        /// </returns>
        /// <exception cref="ValidationException">다음 상황에서 발생:
        /// - ErrorCode.OAUTH2_REQUEST_NOT_FOUND: state로 저장된 요청을 찾을 수 없을 때.
        /// - ErrorCode.OAUTH2_TOKEN_EXCHANGE_FAILED: authorization code로 토큰 교환에 실패했을 때.
        /// - ErrorCode.OAUTH2_USER_INFO_FAILED: 공급자에서 가져온 사용자 정보에 ID가 없을 때.
        /// </exception>
        public async Task<OAuth2CallbackResult> HandleOAuth2CallbackAsync(string code, string state)
        {
            var authRequest = await GetOAuth2RequestAsync(state);
            if (authRequest == null) {
                throw new ValidationException(ErrorCode.OAUTH2_REQUEST_NOT_FOUND);
            }

            var tokenResponse = await ExchangeAuthorizationCodeAsync(
                code,
                authRequest.ClientId,
                authRequest.RedirectUri,
                authRequest.CodeVerifier);

            if (!tokenResponse.Success) {
                throw new ValidationException(ErrorCode.OAUTH2_TOKEN_EXCHANGE_FAILED);
            }

            await DeleteOAuth2RequestAsync(state);

            var providerName = GetProviderNameFromClientId(authRequest.ClientId);
            var userInfo = await GetUserInfoAsync(tokenResponse.Tokens!.AccessToken, providerName);

            if (string.IsNullOrEmpty(userInfo.Id)) {
                throw new ValidationException(ErrorCode.OAUTH2_USER_INFO_FAILED);
            }

            var authResult = await _authService.LoginWithOAuthAsync(providerName, userInfo.Id);

            var tokenData = new OAuth2TokenData {
                AccessToken = authResult.Tokens!.AccessToken,
                RefreshToken = authResult.Tokens.RefreshToken,
                ExpiresIn = (int)(authResult.Tokens.AccessTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
                UID = authResult.User!.UID
            };

            await StoreTokenDataAsync(state, tokenData);

            var clientRedirectUrl = $"{authRequest.ClientRedirectUri}?" +
                                   $"success=true&" +
                                   $"state={Uri.EscapeDataString(state)}";

            return new OAuth2CallbackResult {
                Success = true,
                RedirectUrl = clientRedirectUrl
            };
        }

        /// <summary>
        /// 주어진 인증 코드로 제공자 토큰 엔드포인트에 교환 요청을 수행하여 액세스/리프레시 토큰을 반환합니다.
        /// </summary>
        /// <remarks>
        /// clientId로 구성된 제공자를 찾아 토큰 교환 요청을 구성하고 POST합니다. PKCE를 사용하는 경우 <paramref name="codeVerifier"/>를 포함합니다.
        /// </remarks>
        /// <param name="code">OAuth2 인증 서버가 콜백으로 제공한 authorization code.</param>
        /// <param name="clientId">요청에 사용되는 클라이언트 ID(설정에서 제공자 식별에 사용됨).</param>
        /// <param name="redirectUri">토큰 교환에 사용된 리디렉션 URI(인증 요청과 동일해야 함).</param>
        /// <param name="codeVerifier">선택적 PKCE code_verifier(사용하는 경우 전달).</param>
        /// <returns>교환이 성공하면 Success=true 및 Tokens에 액세스/리프레시 토큰과 만료 정보를 포함한 TokenResponse.</returns>
        /// <exception cref="ValidationException">clientId가 구성에서 발견되지 않을 경우(OAUTH2_CLIENT_ID_INVALID).</exception>
        /// <exception cref="ExternalServiceException">토큰 엔드포인트 호출이 실패한 경우(OAUTH2_TOKEN_EXCHANGE_FAILED).</exception>
        public async Task<TokenResponse> ExchangeAuthorizationCodeAsync(string code, string clientId, string redirectUri, string codeVerifier = "")
        {
            var providerSettings = GetProviderByClientId(clientId);
            if (providerSettings == null) {
                throw new ValidationException(ErrorCode.OAUTH2_CLIENT_ID_INVALID);
            }

            var providerName = GetProviderName(clientId);
            var provider = _providerFactory.GetProvider(providerName);

            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", providerSettings.ClientId },
                { "client_secret", providerSettings.ClientSecret }
            };

            if (!string.IsNullOrEmpty(codeVerifier)) {
                parameters.Add("code_verifier", codeVerifier);
            }

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(provider.TokenEndpoint, content);

            if (response.IsSuccessStatusCode) {
                var json = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                return new TokenResponse {
                    Success = true,
                    Tokens = new Tokens {
                        AccessToken = tokenData!["access_token"].GetString()!,
                        RefreshToken = tokenData["refresh_token"].GetString()!,
                        ExpiresIn = tokenData["expires_in"].GetInt32(),
                        TokenType = tokenData["token_type"].GetString()!
                    }
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("OAuth2 토큰 교환 실패: {Error}", errorContent);
            throw new ExternalServiceException(
                "OAuth2",
                provider.TokenEndpoint,
                $"OAuth2 토큰 교환 실패: {errorContent}",
                ErrorCode.OAUTH2_TOKEN_EXCHANGE_FAILED
            );
        }

        /// <summary>
        /// 지정된 OAuth2 공급자에서 액세스 토큰으로 사용자 정보를 조회하여 공급자별 파싱 결과를 반환합니다.
        /// </summary>
        /// <param name="accessToken">요청에 사용할 Bearer 액세스 토큰.</param>
        /// <param name="providerName">IOAuth2ProviderFactory에서 조회할 공급자 식별자(예: "google").</param>
        /// <returns>공급자 구현에 의해 파싱된 OAuth2UserInfo 객체.</returns>
        /// <exception cref="ExternalServiceException">공급자의 사용자 정보 엔드포인트 호출이 실패할 경우 던져집니다.</exception>
        public async Task<OAuth2UserInfo> GetUserInfoAsync(string accessToken, string providerName)
        {
            var provider = _providerFactory.GetProvider(providerName);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync(provider.UserInfoEndpoint);
            if (response.IsSuccessStatusCode) {
                var json = await response.Content.ReadAsStringAsync();
                return provider.ParseUserInfo(json);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("OAuth2 사용자 정보 조회 실패: {Error}", errorContent);
            throw new ExternalServiceException(
                "OAuth2",
                provider.UserInfoEndpoint,
                $"OAuth2 사용자 정보 조회 실패: {errorContent}",
                ErrorCode.OAUTH2_USER_INFO_FAILED
            );
        }

        /// <summary>
        /// 지정된 상태(state)에 대한 OAuth2 인증 요청을 직렬화하여 인메모리 저장소에 보관합니다.
        /// </summary>
        /// <param name="state">요청을 식별하는 고유한 상태 토큰.</param>
        /// <param name="request">저장할 OAuth2 인증 요청 객체.</param>
        /// <returns>저장된 동일한 <c>OAuth2AuthRequest</c> 객체를 반환합니다.</returns>
        public async Task<OAuth2AuthRequest> StoreOAuth2RequestAsync(string state, OAuth2AuthRequest request)
        {
            try
            {
                _oauth2Requests[state] = JsonSerializer.Serialize(request);
                await Task.CompletedTask;
                return request;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OAuth2 요청 저장 실패: State={State}", state);
                throw;
            }
        }

        /// <summary>
        /// 주어진 상태(state)에 연관된 저장된 OAuth2 인증 요청을 조회하여 역직렬화된 OAuth2AuthRequest를 반환합니다.
        /// </summary>
        /// <param name="state">조회할 OAuth2 인증 요청과 연관된 상태 문자열(일치하는 키).</param>
        /// <returns>찾으면 역직렬화된 <see cref="OAuth2AuthRequest"/> 인스턴스, 없으면 null을 반환합니다.</returns>
        public async Task<OAuth2AuthRequest?> GetOAuth2RequestAsync(string state)
        {            
            if (_oauth2Requests.TryGetValue(state, out var json)) {
                var request = JsonSerializer.Deserialize<OAuth2AuthRequest>(json);
                await Task.CompletedTask;
                return request;
            }
            
            await Task.CompletedTask;
            return null;
        }

        /// <summary>
        /// 지정된 상태 키에 연관된 저장된 OAuth2 인증 요청을 삭제합니다.
        /// </summary>
        /// <param name="state">삭제할 OAuth2 인증 요청을 식별하는 상태(state) 문자열 키.</param>
        public async Task DeleteOAuth2RequestAsync(string state)
        {
            _oauth2Requests.Remove(state);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 주어진 토큰 관련 데이터를 JSON으로 직렬화하여 서비스의 인메모리 토큰 저장소에 지정된 state 키로 저장합니다.
        /// </summary>
        /// <param name="state">토큰을 식별할 클라이언트 상태값(state) 문자열.</param>
        /// <param name="tokenData">저장할 토큰 정보(예: 액세스/리프레시 토큰, 만료 정보 등)를 포함한 객체.</param>
        public async Task StoreTokenDataAsync(string state, object tokenData)
        {
            _tokenData[state] = JsonSerializer.Serialize(tokenData);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 지정한 OAuth2 상태 키와 연관된 저장된 토큰 데이터를 삭제합니다.
        /// </summary>
        /// <param name="state">삭제할 토큰 데이터가 저장된 OAuth2 요청의 상태(state) 키.</param>
        public async Task DeleteTokenDataAsync(string state)
        public async Task DeleteTokenDataAsync(string state)
        {
            _tokenData.Remove(state);
            await Task.CompletedTask;
        }

        /// <summary>
        /// 주어진 상태(state)에 연관된 저장된 OAuth2 토큰 데이터를 조회합니다.
        /// </summary>
        /// <param name="state">OAuth2 인증 흐름에서 사용한 상태 값(state) — 토큰 데이터의 키로 사용됩니다.</param>
        /// <returns>해당 상태에 연관된 <see cref="OAuth2TokenData"/> 인스턴스 또는 존재하지 않으면 <c>null</c>.</returns>
        public async Task<OAuth2TokenData?> GetTokenDataAsync(string state)
        {
            if (_tokenData.TryGetValue(state, out var json)) {
                await Task.CompletedTask;
                return JsonSerializer.Deserialize<OAuth2TokenData>(json);
            }
            await Task.CompletedTask;
            return null;
        }

        /// <summary>
        /// 지정된 클라이언트 ID와 일치하는 OAuth2 공급자 설정을 조회합니다.
        /// </summary>
        /// <param name="clientId">조회할 공급자의 클라이언트 ID.</param>
        /// <returns>클라이언트 ID에 매칭되는 <see cref="OAuth2Settings"/> 객체 또는 찾을 수 없으면 <c>null</c>.</returns>
        private OAuth2Settings? GetProviderByClientId(string clientId)
        {
            return _settings.Providers.Values.FirstOrDefault(p => p.ClientId == clientId);
        }

        /// <summary>
        /// 주어진 클라이언트 ID에 매핑된 OAuth2 제공자 이름을 반환합니다.
        /// </summary>
        /// <param name="clientId">조회할 제공자의 클라이언트 ID.</param>
        /// <returns>클라이언트 ID에 매칭되는 제공자 이름. 매칭되는 항목이 없으면 기본값 "google"을 반환합니다.</returns>
        private string GetProviderName(string clientId)
        {
            var provider = _settings.Providers.FirstOrDefault(p => p.Value.ClientId == clientId);
            return provider.Key ?? "google";
        }

        /// <summary>
        /// 지정된 클라이언트 ID에 대응하는 OAuth2 공급자 이름을 반환합니다. 구성된 프로바이더 목록을 검색하여 일치하는 항목을 찾습니다.
        /// 기본값은 찾지 못한 경우 "google"입니다.
        /// </summary>
        /// <param name="clientId">검색할 공급자의 클라이언트 ID.</param>
        /// <returns>클라이언트 ID에 매핑된 공급자 이름, 없으면 "google".</returns>
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
