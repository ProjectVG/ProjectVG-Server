using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Services.Auth;
using Microsoft.Extensions.Options;
using ProjectVG.Common.Configuration;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class OAuthController : ControllerBase
    {
        private readonly IOAuth2Service _oauth2Service;

        public OAuthController(
            IOAuth2Service oauth2Service,
            IOptions<OAuth2ProviderSettings> oauth2Settings)
        {
            _oauth2Service = oauth2Service;
        }

        [HttpGet("oauth2/authorize")]
        public async Task<IActionResult> OAuth2Authorize(
            [FromQuery] string state,
            [FromQuery] string code_challenge,
            [FromQuery] string code_challenge_method,
            [FromQuery] string code_verifier,
            [FromQuery] string client_redirect_uri)
        {
            if (string.IsNullOrEmpty(code_challenge) || code_challenge_method != "S256") {
                throw new ValidationException(ErrorCode.OAUTH2_PKCE_INVALID);
            }

            var googleAuthUrl = await _oauth2Service.BuildAuthorizationUrlAsync(state, code_challenge, code_challenge_method, code_verifier, client_redirect_uri);

            return Ok(new {
                success = true,
                auth_url = googleAuthUrl
            });
        }

        [HttpGet("oauth2/callback")]
        [HttpGet("google/callback")]
        public async Task<IActionResult> OAuth2Callback(
            [FromQuery] string code,
            [FromQuery] string state,
            [FromQuery] string error = null)
        {
            if (!string.IsNullOrEmpty(error)) {
                throw new ValidationException(ErrorCode.OAUTH2_CALLBACK_FAILED);
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state)) {
                throw new ValidationException(ErrorCode.REQUIRED_PARAMETER_MISSING);
            }

            var result = await _oauth2Service.HandleOAuth2CallbackAsync(code, state);

            return Redirect(result.RedirectUrl!);
        }

        [HttpGet("oauth2/token")]
        public async Task<IActionResult> GetOAuth2Token([FromQuery] string state)
        {
            if (string.IsNullOrEmpty(state)) {
                throw new ValidationException(ErrorCode.REQUIRED_PARAMETER_MISSING);
            }

            var tokenData = await _oauth2Service.GetTokenDataAsync(state);
            if (tokenData == null) {
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
