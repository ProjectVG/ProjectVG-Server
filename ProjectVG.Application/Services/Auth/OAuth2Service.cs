using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProjectVG.Common.Configuration;
using ProjectVG.Application.Models.Auth;



namespace ProjectVG.Application.Services.Auth
{
    public class OAuth2Service : IOAuth2Service
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OAuth2Service> _logger;
        private readonly OAuth2ProviderSettings _settings;
        private readonly IAuthService _authService;
        private readonly Dictionary<string, string> _oauth2Requests = new();
        private readonly Dictionary<string, string> _tokenData = new();

        public OAuth2Service(
            IHttpClientFactory httpClientFactory,
            ILogger<OAuth2Service> logger,
            IOptions<OAuth2ProviderSettings> settings,
            IAuthService authService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _settings = settings.Value;
            _authService = authService;
        }

        public async Task<string> BuildAuthorizationUrlAsync(string state, string codeChallenge, string codeChallengeMethod, string codeVerifier, string clientRedirectUri)
        {
            if (!_settings.Providers.TryGetValue("google", out var googleProvider) || !googleProvider.Enabled) {
                throw new ValidationException(ErrorCode.OAUTH2_PROVIDER_NOT_CONFIGURED);
            }

            if (string.IsNullOrEmpty(googleProvider.ClientId)) {
                throw new ValidationException(ErrorCode.OAUTH2_CLIENT_ID_INVALID);
            }

            var authRequest = new OAuth2AuthRequest {
                ClientId = googleProvider.ClientId,
                RedirectUri = googleProvider.RedirectUri,
                ClientRedirectUri = clientRedirectUri,
                State = state,
                CodeChallenge = codeChallenge,
                CodeVerifier = codeVerifier,
                CodeChallengeMethod = codeChallengeMethod,
                CreatedAt = DateTime.UtcNow
            };

            await StoreOAuth2RequestAsync(state, authRequest);

            var googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth" +
                                $"?client_id={Uri.EscapeDataString(googleProvider.ClientId)}" +
                                $"&redirect_uri={Uri.EscapeDataString(googleProvider.RedirectUri)}" +
                                $"&response_type=code" +
                                $"&scope={Uri.EscapeDataString("openid email profile")}" +
                                $"&state={state}" +
                                $"&code_challenge={codeChallenge}" +
                                $"&code_challenge_method={codeChallengeMethod}";

            return googleAuthUrl;
        }

        public async Task<TokenResponse> ExchangeAuthorizationCodeAsync(string code, string clientId, string redirectUri, string codeVerifier = "")
        {
            var provider = GetProviderByClientId(clientId);
            if (provider == null)
            {
                throw new ValidationException(ErrorCode.OAUTH2_CLIENT_ID_INVALID);
            }

            var providerName = GetProviderName(clientId);
            var tokenEndpoint = GetTokenEndpoint(providerName);
            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "client_id", provider.ClientId },
                { "client_secret", provider.ClientSecret }
            };

            if (!string.IsNullOrEmpty(codeVerifier))
            {
                parameters.Add("code_verifier", codeVerifier);
            }

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(tokenEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var tokenData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                return new TokenResponse
                {
                    Success = true,
                    Tokens = new Tokens
                    {
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
                tokenEndpoint,
                $"OAuth2 토큰 교환 실패: {errorContent}",
                ErrorCode.OAUTH2_TOKEN_EXCHANGE_FAILED
            );
        }


        public async Task<OAuth2UserInfo> GetUserInfoAsync(string accessToken, string provider)
        {
            var userInfoEndpoint = GetUserInfoEndpoint(provider);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync(userInfoEndpoint);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var userData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                return new OAuth2UserInfo
                {
                    Id = userData!["id"].GetString()!,
                    Email = userData["email"].GetString()!,
                    Provider = provider
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("OAuth2 사용자 정보 조회 실패: {Error}", errorContent);
            throw new ExternalServiceException(
                "OAuth2",
                userInfoEndpoint,
                $"OAuth2 사용자 정보 조회 실패: {errorContent}",
                ErrorCode.OAUTH2_USER_INFO_FAILED
            );
        }

        public async Task<OAuth2AuthRequest> StoreOAuth2RequestAsync(string state, OAuth2AuthRequest request)
        {
            _oauth2Requests[state] = JsonSerializer.Serialize(request);
            await Task.CompletedTask;
            return request;
        }

        public async Task<OAuth2AuthRequest?> GetOAuth2RequestAsync(string state)
        {
            if (_oauth2Requests.TryGetValue(state, out var json))
            {
                await Task.CompletedTask;
                return JsonSerializer.Deserialize<OAuth2AuthRequest>(json);
            }
            await Task.CompletedTask;
            return null;
        }

        public async Task DeleteOAuth2RequestAsync(string state)
        {
            _oauth2Requests.Remove(state);
            await Task.CompletedTask;
        }

        public async Task StoreTokenDataAsync(string state, object tokenData)
        {
            _tokenData[state] = JsonSerializer.Serialize(tokenData);
            await Task.CompletedTask;
        }


        public async Task DeleteTokenDataAsync(string state)
        {
            _tokenData.Remove(state);
            await Task.CompletedTask;
        }

        private OAuth2Settings? GetProviderByClientId(string clientId)
        {
            return _settings.Providers.Values.FirstOrDefault(p => p.ClientId == clientId);
        }

        private string GetProviderName(string clientId)
        {
            var provider = _settings.Providers.FirstOrDefault(p => p.Value.ClientId == clientId);
            return provider.Key ?? "unknown";
        }

        private string GetTokenEndpoint(string provider)
        {
            return provider switch
            {
                "google" => "https://oauth2.googleapis.com/token",
                "github" => "https://github.com/login/oauth/access_token",
                "microsoft" => "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                _ => throw new ArgumentException($"Unsupported provider: {provider}")
            };
        }

        private string GetUserInfoEndpoint(string provider)
        {
            return provider switch
            {
                "google" => "https://www.googleapis.com/oauth2/v2/userinfo",
                "github" => "https://api.github.com/user",
                "microsoft" => "https://graph.microsoft.com/v1.0/me",
                _ => throw new ArgumentException($"Unsupported provider: {provider}")
            };
        }

        private string GetProviderNameFromClientId(string clientId)
        {
            foreach (var provider in _settings.Providers)
            {
                if (provider.Value.ClientId == clientId)
                {
                    return provider.Key;
                }
            }
            return "google";
        }


        public async Task<OAuth2CallbackResult> HandleOAuth2CallbackAsync(string code, string state)
        {
            var authRequest = await GetOAuth2RequestAsync(state);
            if (authRequest == null)
            {
                throw new ValidationException(ErrorCode.OAUTH2_REQUEST_NOT_FOUND);
            }

            var tokenResponse = await ExchangeAuthorizationCodeAsync(
                code,
                authRequest.ClientId,
                authRequest.RedirectUri,
                authRequest.CodeVerifier);

            if (!tokenResponse.Success)
            {
                throw new ValidationException(ErrorCode.OAUTH2_TOKEN_EXCHANGE_FAILED);
            }

            await DeleteOAuth2RequestAsync(state);

            var providerName = GetProviderNameFromClientId(authRequest.ClientId);
            var userInfo = await GetUserInfoAsync(tokenResponse.Tokens!.AccessToken, providerName);

            if (string.IsNullOrEmpty(userInfo.Id))
            {
                throw new ValidationException(ErrorCode.OAUTH2_USER_INFO_FAILED);
            }

            var authResult = await _authService.LoginWithOAuthAsync(providerName, userInfo.Id);

            var tokenData = new OAuth2TokenData
            {
                AccessToken = authResult.Tokens!.AccessToken,
                RefreshToken = authResult.Tokens.RefreshToken,
                ExpiresIn = (int)(authResult.Tokens.AccessTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
                UID = authResult.User!.UID
            };

            await StoreTokenDataAsync(state, tokenData);

            var clientRedirectUrl = $"{authRequest.ClientRedirectUri}?" +
                                   $"success=true&" +
                                   $"state={Uri.EscapeDataString(state)}";

            return new OAuth2CallbackResult
            {
                Success = true,
                RedirectUrl = clientRedirectUrl
            };
        }

        public async Task<OAuth2TokenData?> GetTokenDataAsync(string state)
        {
            if (_tokenData.TryGetValue(state, out var json))
            {
                await Task.CompletedTask;
                return JsonSerializer.Deserialize<OAuth2TokenData>(json);
            }
            await Task.CompletedTask;
            return null;
        }

    }
}
