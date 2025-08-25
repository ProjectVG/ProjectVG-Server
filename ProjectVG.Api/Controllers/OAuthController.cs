using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Application.Models.Auth;
using ProjectVG.Infrastructure.Auth;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ProjectVG.Common.Configuration;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class OAuthController : ControllerBase
    {
        private readonly IOAuth2Service _oauth2Service;
        private readonly IAuthService _authService;
        private readonly OAuth2ProviderSettings _oauth2Settings;

        public OAuthController(
            IHttpClientFactory httpClientFactory, 
            JwtService jwtService,
            IOAuth2Service oauth2Service,
            IAuthService authService,
            IOptions<OAuth2ProviderSettings> oauth2Settings)
        {
            _oauth2Service = oauth2Service;
            _authService = authService;
            _oauth2Settings = oauth2Settings.Value;
        }

        [HttpGet("oauth2/authorize")]
        public async Task<IActionResult> OAuth2Authorize(
            [FromQuery] string scope,
            [FromQuery] string state,
            [FromQuery] string code_challenge,
            [FromQuery] string code_challenge_method,
            [FromQuery] string code_verifier,
            [FromQuery] string client_redirect_uri)
        {
            try
            {
                if (string.IsNullOrEmpty(code_challenge) || code_challenge_method != "S256")
                {
                    return BadRequest("Invalid PKCE parameters");
                }

                var googleAuthUrl = await _oauth2Service.BuildAuthorizationUrlAsync(scope, state, code_challenge, code_challenge_method, code_verifier, client_redirect_uri);
                
                return Ok(new
                {
                    success = true,
                    auth_url = googleAuthUrl,
                    state = state,
                    message = "Redirect to this URL to start OAuth2 login"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("OAuth2 authorization failed");
            }
        }

        [HttpGet("oauth2/callback")]
        [HttpGet("google/callback")]
        public async Task<IActionResult> OAuth2Callback(
            [FromQuery] string code,
            [FromQuery] string state,
            [FromQuery] string error = null)
        {
            if (!string.IsNullOrEmpty(error))
            {
                return BadRequest(new { success = false, message = $"OAuth2 error: {error}" });
            }

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                return BadRequest(new { success = false, message = "Missing required parameters: code or state" });
            }

            var result = await _oauth2Service.HandleOAuth2CallbackAsync(code, state);
            
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Redirect(result.RedirectUrl!);
        }

        [HttpPost("oauth2/exchange")]
        public async Task<IActionResult> ExchangeOAuth2Token([FromBody] ExchangeTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ExchangeToken))
                {
                    return BadRequest(new { success = false, message = "Exchange token is required" });
                }

                var tokenData = await _oauth2Service.ExchangeTokenAsync(request.ExchangeToken, HttpContext);
                if (tokenData == null)
                {
                    return BadRequest(new { success = false, message = "Invalid or expired exchange token" });
                }

                Console.WriteLine($"[OAuth2 Exchange] User UID: {tokenData.UID}");
                Console.WriteLine($"[OAuth2 Exchange] Access Token: {tokenData.AccessToken[..Math.Min(20, tokenData.AccessToken.Length)]}...");
                Console.WriteLine($"[OAuth2 Exchange] Refresh Token: {tokenData.RefreshToken[..Math.Min(20, tokenData.RefreshToken.Length)]}...");
                Console.WriteLine($"[OAuth2 Exchange] Expires In: {tokenData.ExpiresIn} seconds");

                Response.Headers.Append("X-Access-Token", tokenData.AccessToken);
                Response.Headers.Append("X-Refresh-Token", tokenData.RefreshToken);
                Response.Headers.Append("X-Expires-In", tokenData.ExpiresIn.ToString());
                Response.Headers.Append("X-UID", tokenData.UID);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

    }
}
