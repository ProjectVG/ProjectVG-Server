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

        /// <summary>
        /// OAuth2 공급자 팩토리의 기본 생성자입니다.
        /// </summary>
        /// <remarks>
        /// 내부적으로 공급자 이름을 대소문자 구분 없이 비교하는 사전(Dictionary)을 초기화하고,
        /// 기본 제공 OAuth2 공급자("google", "apple") 인스턴스를 등록합니다.
        /// </remarks>
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
        /// <summary>
        /// 지정한 이름에 해당하는 OAuth2 제공자(IOAuth2Provider)를 반환합니다.
        /// </summary>
        /// <param name="providerName">조회할 제공자 이름(대소문자 구분 없이 비교됨). 비어 있거나 null일 수 없습니다.</param>
        /// <returns>요청한 이름에 매칭되는 IOAuth2Provider 인스턴스.</returns>
        /// <exception cref="ValidationException">providerName이 null/빈 값이면 ErrorCode.OAUTH2_PROVIDER_NOT_CONFIGURED, 등록되지 않은 제공자 이름이면 ErrorCode.OAUTH2_PROVIDER_NOT_SUPPORTED를 포함하여 발생합니다.</exception>
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
        /// <summary>
        /// 팩토리가 현재 등록하고 있는 OAuth2 제공자 이름들의 컬렉션을 반환합니다.
        /// </summary>
        /// <returns>등록된 제공자 이름들의 읽기 전용 컬렉션(딕셔너리의 키 컬렉션).</returns>
        public IEnumerable<string> GetSupportedProviders()
        {
            return _providers.Keys;
        }

        /// <summary>
        /// 제공자가 지원되는지 확인
        /// </summary>
        /// <param name="providerName">확인할 제공자 이름</param>
        /// <summary>
        /// 지정된 OAuth2 제공자가 공장에 등록되어 있는지 확인합니다. 대소문자 구분 없이 비교합니다.
        /// </summary>
        /// <param name="providerName">확인할 제공자 이름(빈 문자열 또는 null이면 false를 반환).</param>
        /// <returns>등록되어 있으면 <c>true</c>, 그렇지 않으면 <c>false</c>.</returns>
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
        /// <summary>
/// 지정한 이름에 해당하는 OAuth2 제공자 인스턴스를 반환합니다.
/// </summary>
/// <param name="providerName">조회할 제공자 이름(빈 문자열 또는 null이면 예외).</param>
/// <returns>요청한 이름에 매칭되는 <see cref="IOAuth2Provider"/> 인스턴스.</returns>
/// <exception cref="ValidationException">
/// providerName이 null 또는 빈 문자열일 때(<see cref="ErrorCode.OAUTH2_PROVIDER_NOT_CONFIGURED"/>) 
/// 또는 등록되지 않은 제공자 이름일 때(<see cref="ErrorCode.OAUTH2_PROVIDER_NOT_SUPPORTED"/>).
/// </exception>
        IOAuth2Provider GetProvider(string providerName);

        /// <summary>
        /// 지원하는 모든 제공자 목록 조회
        /// </summary>
        /// <summary>
/// 팩토리에 등록된 OAuth2 제공자들의 이름을 반환합니다(대소문자 구분 없음).
/// </summary>
/// <returns>등록된 제공자 이름(string)의 열거형 컬렉션</returns>
        IEnumerable<string> GetSupportedProviders();

        /// <summary>
        /// 제공자가 지원되는지 확인
        /// </summary>
        /// <param name="providerName">확인할 제공자 이름</param>
        /// <summary>
/// 지정한 OAuth2 제공자가 팩토리에 등록되어 있고 사용할 수 있는지 확인합니다.
/// </summary>
/// <param name="providerName">확인할 공급자 이름(빈 문자열 또는 null이면 false를 반환).</param>
/// <returns>대소문자를 구분하지 않는 비교로 등록된 제공자이면 true, 그렇지 않으면 false.</returns>
        bool IsProviderSupported(string providerName);
    }
}
