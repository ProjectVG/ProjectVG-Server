using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Common.Configuration;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class OAuthController : ControllerBase
    {
        private readonly IOAuth2Service _oauth2Service;
        private readonly IOAuth2ProviderFactory _providerFactory;

        /// <summary>
        /// OAuth2 관련 서비스와 공급자 팩토리를 사용하도록 컨트롤러를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 생성자에서 전달된 `IOptions&lt;OAuth2ProviderSettings&gt; oauth2Settings` 값은 현재 필드로 저장되거나 사용되지 않습니다.
        /// </remarks>
        public OAuthController(
            IOAuth2Service oauth2Service,
            IOAuth2ProviderFactory providerFactory,
            IOptions<OAuth2ProviderSettings> oauth2Settings)
        {
            _oauth2Service = oauth2Service;
            _providerFactory = providerFactory;
        }

        /// <summary>
        /// 지원되는 OAuth2 제공자 목록을 조회하여 성공 여부와 함께 반환합니다.
        /// </summary>
        /// <returns>HTTP 200 응답으로 { success = true, providers = [...] } 형태의 JSON 결과를 포함한 IActionResult를 반환합니다.</returns>
        [HttpGet("oauth2/providers")]
        public IActionResult GetSupportedProviders()
        {
            var providers = _providerFactory.GetSupportedProviders();
            return Ok(new
            {
                success = true,
                providers = providers.ToList()
            });
        }

        /// <summary>
        /// 지정된 OAuth2 공급자에 대한 PKCE 검증을 수행하고, 공급자별 인증 URL을 생성하여 반환합니다.
        /// </summary>
        /// <param name="provider">요청할 OAuth2 공급자 이름. 지원되지 않는 공급자면 ValidationException(ErrorCode.OAUTH2_PROVIDER_NOT_SUPPORTED)이 발생합니다.</param>
        /// <param name="state">클라이언트에서 전달한 상태 토큰(예: CSRF/상태 검증용 식별자).</param>
        /// <param name="code_challenge">PKCE 코드 챌린지(필수). 비어 있으면 ValidationException(ErrorCode.OAUTH2_PKCE_INVALID)이 발생합니다.</param>
        /// <param name="code_challenge_method">PKCE 메서드(현재 "S256"만 허용). 다른 값이면 ValidationException(ErrorCode.OAUTH2_PKCE_INVALID)이 발생합니다.</param>
        /// <param name="code_verifier">옵션인 PKCE 코드 베리파이어(후속 흐름에서 사용될 수 있음).</param>
        /// <param name="client_redirect_uri">클라이언트가 원하는 리디렉션 URI(옵션, 공급자별 처리).</param>
        /// <returns>HTTP 200 응답으로 { success = true, provider, auth_url } 형태의 JSON을 반환합니다. auth_url은 클라이언트가 리디렉션해야 할 공급자 인증 URL입니다.</returns>
        /// <exception cref="ValidationException">지원되지 않는 공급자 또는 PKCE 검증 실패 시 발생합니다. 사용되는 ErrorCode: OAUTH2_PROVIDER_NOT_SUPPORTED, OAUTH2_PKCE_INVALID.</exception>
        [HttpGet("oauth2/authorize/{provider}")]
        public async Task<IActionResult> OAuth2Authorize(
            string provider,
            [FromQuery] string state,
            [FromQuery] string code_challenge,
            [FromQuery] string code_challenge_method,
            [FromQuery] string code_verifier,
            [FromQuery] string client_redirect_uri)
        {
            if (!_providerFactory.IsProviderSupported(provider))
            {
                throw new ValidationException(ErrorCode.OAUTH2_PROVIDER_NOT_SUPPORTED);
            }

            if (string.IsNullOrEmpty(code_challenge) || code_challenge_method != "S256")
            {
                throw new ValidationException(ErrorCode.OAUTH2_PKCE_INVALID);
            }

            // 제공자별 인증 URL 생성
            var authUrl = await _oauth2Service.BuildAuthorizationUrlAsync(provider, state, code_challenge, code_challenge_method, code_verifier, client_redirect_uri);

            return Ok(new
            {
                success = true,
                provider = provider,
                auth_url = authUrl
            });
        }

        /// <summary>
        /// OAuth2 콜백 엔드포인트를 처리하고, 처리 결과의 리디렉션 URL로 리다이렉트합니다.
        /// </summary>
        /// <param name="provider">선택적 공급자 식별자(경로 매개변수). 제공되지 않아도 됩니다.</param>
        /// <param name="code">OAuth2 공급자가 반환한 인증 코드(쿼리). 필수입니다.</param>
        /// <param name="state">요청 시 전달된 상태 값(쿼리). 필수입니다.</param>
        /// <param name="error">공급자가 반환한 오류 메시지(쿼리). 기본값은 null입니다. 존재하면 예외가 발생합니다.</param>
        /// <returns>처리 결과의 RedirectUrl로 리다이렉트하는 <see cref="IActionResult"/>를 반환합니다.</returns>
        /// <exception cref="ValidationException">
        /// error 파라미터가 비어있지 않거나(code/state가 누락된 경우) 검증 실패 시 각각 다음 오류 코드를 발생시킵니다:
        /// - OAUTH2_CALLBACK_FAILED: callback에서 error가 전달된 경우
        /// - REQUIRED_PARAMETER_MISSING: code 또는 state가 누락된 경우
        /// </exception>
        [HttpGet("oauth2/callback")]
        [HttpGet("oauth2/callback/{provider}")]
        public async Task<IActionResult> OAuth2Callback(
            string? provider,
            [FromQuery] string code,
            [FromQuery] string state,
            [FromQuery] string error = null)
        {
            if (!string.IsNullOrEmpty(error))
            {
                throw new ValidationException(ErrorCode.OAUTH2_CALLBACK_FAILED);
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                throw new ValidationException(ErrorCode.REQUIRED_PARAMETER_MISSING);
            }

            var result = await _oauth2Service.HandleOAuth2CallbackAsync(code, state);

            return Redirect(result.RedirectUrl!);
        }

        /// <summary>
        /// 주어진 상태(state)에 대응하는 OAuth2 토큰 데이터를 조회하여 응답 헤더로 반환하고 해당 토큰 데이터를 삭제합니다.
        /// </summary>
        /// <remarks>
        /// - 요청 쿼리의 <c>state</c>가 필수입니다.
        /// - 조회한 토큰 데이터는 반환 직후 삭제됩니다.
        /// - 반환 시 다음 헤더를 추가합니다: <c>X-Access-Token</c>, <c>X-Refresh-Token</c>, <c>X-Expires-In</c>, <c>X-UID</c>.
        /// </remarks>
        /// <param name="state">토큰 조회를 식별하는 상태 식별자(쿼리 파라미터). 빈값이면 예외가 발생합니다.</param>
        /// <returns>요청이 성공하면 HTTP 200과 { success = true }를 반환합니다.</returns>
        /// <exception cref="ValidationException">다음 상황에서 발생합니다:
        /// - <see cref="ErrorCode.REQUIRED_PARAMETER_MISSING"/>: state가 비어있을 때.
        /// - <see cref="ErrorCode.OAUTH2_REQUEST_NOT_FOUND"/>: 해당 state에 대한 토큰 데이터가 없을 때.
        /// </exception>
        [HttpGet("oauth2/token")]
        public async Task<IActionResult> GetOAuth2Token([FromQuery] string state)
        {
            if (string.IsNullOrEmpty(state))
            {
                throw new ValidationException(ErrorCode.REQUIRED_PARAMETER_MISSING);
            }

            var tokenData = await _oauth2Service.GetTokenDataAsync(state);
            if (tokenData == null)
            {
                throw new ValidationException(ErrorCode.OAUTH2_REQUEST_NOT_FOUND);
            }

            await _oauth2Service.DeleteTokenDataAsync(state);

            Response.Headers.Append("X-Access-Token", tokenData.AccessToken);
            Response.Headers.Append("X-Refresh-Token", tokenData.RefreshToken);
            Response.Headers.Append("X-Expires-In", tokenData.ExpiresIn.ToString());
            Response.Headers.Append("X-UID", tokenData.UID);

            return Ok(new { success = true });
        }
    }
}
