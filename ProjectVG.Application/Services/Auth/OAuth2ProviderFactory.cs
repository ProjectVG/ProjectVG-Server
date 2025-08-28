using ProjectVG.Application.Services.Auth.Providers;
using ProjectVG.Common.Exceptions;
using ProjectVG.Common.Constants;

namespace ProjectVG.Application.Services.Auth
{
    /// <summary>
    /// OAuth2 제공자 팩토리
    /// 제공자 이름에 따라 적절한 OAuth2 제공자 인스턴스를 생성
    /// </summary>
    public class OAuth2ProviderFactory : IOAuth2ProviderFactory
    {
        private readonly Dictionary<string, IOAuth2Provider> _providers;

        public OAuth2ProviderFactory()
        {
            _providers = new Dictionary<string, IOAuth2Provider>(StringComparer.OrdinalIgnoreCase)
            {
                { "google", new GoogleOAuth2Provider() },
                { "apple", new AppleOAuth2Provider() }
            };
        }

        /// <summary>
        /// 제공자 이름으로 OAuth2 제공자 인스턴스 조회
        /// </summary>
        /// <param name="providerName">제공자 이름 (google, apple)</param>
        /// <returns>OAuth2 제공자 인스턴스</returns>
        /// <exception cref="ValidationException">지원하지 않는 제공자인 경우</exception>
        public IOAuth2Provider GetProvider(string providerName)
        {
            if (string.IsNullOrEmpty(providerName)) {
                throw new ValidationException(ErrorCode.OAUTH2_PROVIDER_NOT_CONFIGURED);
            }

            if (!_providers.TryGetValue(providerName, out var provider)) {
                throw new ValidationException(ErrorCode.OAUTH2_PROVIDER_NOT_SUPPORTED);
            }

            return provider;
        }

        /// <summary>
        /// 지원하는 모든 제공자 목록 조회
        /// </summary>
        /// <returns>지원하는 제공자 이름 목록</returns>
        public IEnumerable<string> GetSupportedProviders()
        {
            return _providers.Keys;
        }

        /// <summary>
        /// 제공자가 지원되는지 확인
        /// </summary>
        /// <param name="providerName">확인할 제공자 이름</param>
        /// <returns>지원 여부</returns>
        public bool IsProviderSupported(string providerName)
        {
            return !string.IsNullOrEmpty(providerName) && _providers.ContainsKey(providerName);
        }
    }

    /// <summary>
    /// OAuth2 제공자 팩토리 인터페이스
    /// </summary>
    public interface IOAuth2ProviderFactory
    {
        /// <summary>
        /// 제공자 이름으로 OAuth2 제공자 인스턴스 조회
        /// </summary>
        /// <param name="providerName">제공자 이름</param>
        /// <returns>OAuth2 제공자 인스턴스</returns>
        IOAuth2Provider GetProvider(string providerName);

        /// <summary>
        /// 지원하는 모든 제공자 목록 조회
        /// </summary>
        /// <returns>지원하는 제공자 이름 목록</returns>
        IEnumerable<string> GetSupportedProviders();

        /// <summary>
        /// 제공자가 지원되는지 확인
        /// </summary>
        /// <param name="providerName">확인할 제공자 이름</param>
        /// <returns>지원 여부</returns>
        bool IsProviderSupported(string providerName);
    }
}
