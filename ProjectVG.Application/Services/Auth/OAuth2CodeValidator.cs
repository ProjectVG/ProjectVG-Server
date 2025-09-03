using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectVG.Application.Models.Auth;
using ProjectVG.Common.Configuration;
using ProjectVG.Common.Constants;
using ProjectVG.Common.Exceptions;

namespace ProjectVG.Application.Services.Auth
{
    public class OAuth2CodeValidator : IOAuth2CodeValidator
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OAuth2CodeValidator> _logger;
        private readonly OAuth2ProviderSettings _settings;
        private readonly IOAuth2ProviderFactory _providerFactory;

        public OAuth2CodeValidator(
            IHttpClientFactory httpClientFactory,
            ILogger<OAuth2CodeValidator> logger,
            IOptions<OAuth2ProviderSettings> settings,
            IOAuth2ProviderFactory providerFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _settings = settings.Value;
            _providerFactory = providerFactory;
        }

        public async Task<TokenResponse> ValidateAndExchangeCodeAsync(string code, string clientId, string redirectUri, string codeVerifier = "")
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

        private OAuth2Settings? GetProviderByClientId(string clientId)
        {
            return _settings.Providers.Values.FirstOrDefault(p => p.ClientId == clientId);
        }

        private string GetProviderName(string clientId)
        {
            var provider = _settings.Providers.FirstOrDefault(p => p.Value.ClientId == clientId);
            return provider.Key ?? "google";
        }
    }
}