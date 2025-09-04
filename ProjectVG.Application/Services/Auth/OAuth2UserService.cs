using System.Net.Http.Headers;

namespace ProjectVG.Application.Services.Auth
{
    public class OAuth2UserService : IOAuth2UserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OAuth2UserService> _logger;
        private readonly IOAuth2ProviderFactory _providerFactory;

        public OAuth2UserService(
            IHttpClientFactory httpClientFactory,
            ILogger<OAuth2UserService> logger,
            IOAuth2ProviderFactory providerFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _providerFactory = providerFactory;
        }

        public async Task<OAuth2UserInfo> GetUserInfoAsync(string accessToken, string providerName)
        {
            var provider = _providerFactory.GetProvider(providerName);
            
            using var req = new HttpRequestMessage(HttpMethod.Get, provider.UserInfoEndpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            var response = await _httpClient.SendAsync(req);
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
    }
}