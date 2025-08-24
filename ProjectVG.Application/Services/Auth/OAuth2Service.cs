using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
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
        private readonly Dictionary<string, string> _oauth2Requests = new();
        private readonly Dictionary<string, string> _tokenData = new();

        public OAuth2Service(
            IHttpClientFactory httpClientFactory,
            ILogger<OAuth2Service> logger,
            IOptions<OAuth2ProviderSettings> settings)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<TokenResponse> ExchangeAuthorizationCodeAsync(string code, string clientId, string redirectUri, string codeVerifier = "")
        {
            try
            {
                var provider = GetProviderByClientId(clientId);
                if (provider == null)
                {
                    return new TokenResponse { Success = false, Message = "Invalid client ID" };
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
                _logger.LogError("OAuth2 token exchange failed: {Error}", errorContent);
                return new TokenResponse { Success = false, Message = $"Token exchange failed: {errorContent}" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OAuth2 token exchange error");
                return new TokenResponse { Success = false, Message = "Internal error" };
            }
        }

        public async Task<TokenResponse> RefreshAccessTokenAsync(string refreshToken, string clientId)
        {
            try
            {
                var provider = GetProviderByClientId(clientId);
                if (provider == null)
                {
                    return new TokenResponse { Success = false, Message = "Invalid client ID" };
                }

                var providerName = GetProviderName(clientId);
                var tokenEndpoint = GetTokenEndpoint(providerName);
                var parameters = new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken },
                    { "client_id", provider.ClientId },
                    { "client_secret", provider.ClientSecret }
                };

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

                return new TokenResponse { Success = false, Message = "Token refresh failed" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OAuth2 token refresh error");
                return new TokenResponse { Success = false, Message = "Internal error" };
            }
        }

        public async Task<OAuth2UserInfo> GetUserInfoAsync(string accessToken, string provider)
        {
            try
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
                        Name = userData["name"].GetString()!,
                        Picture = userData.ContainsKey("picture") ? userData["picture"].GetString() : null,
                        Provider = provider
                    };
                }

                return new OAuth2UserInfo();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OAuth2 user info error");
                return new OAuth2UserInfo();
            }
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

        public async Task<object?> GetTokenDataAsync(string state)
        {
            if (_tokenData.TryGetValue(state, out var json))
            {
                await Task.CompletedTask;
                return JsonSerializer.Deserialize<OAuth2TokenData>(json);
            }
            await Task.CompletedTask;
            return null;
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

        public Task StoreSessionAsync(string sessionId, object sessionData, TimeSpan? expiry = null)
        {
            throw new NotImplementedException();
        }

        public Task<object?> GetSessionAsync(string sessionId)
        {
            throw new NotImplementedException();
        }

        public Task DeleteSessionAsync(string sessionId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExtendSessionAsync(string sessionId, TimeSpan expiry)
        {
            throw new NotImplementedException();
        }
    }
}
