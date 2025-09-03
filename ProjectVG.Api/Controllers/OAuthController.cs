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

        public OAuthController(
            IOAuth2Service oauth2Service,
            IOAuth2ProviderFactory providerFactory,
            IOptions<OAuth2ProviderSettings> oauth2Settings)
        {
            _oauth2Service = oauth2Service;
            _providerFactory = providerFactory;
        }

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

            Response.Headers.Append("X-Access-Credit", tokenData.AccessToken);
            Response.Headers.Append("X-Refresh-Credit", tokenData.RefreshToken);
            Response.Headers.Append("X-Expires-In", tokenData.ExpiresIn.ToString());
            Response.Headers.Append("X-UID", tokenData.UID);

            return Ok(new { success = true });
        }
    }
}
