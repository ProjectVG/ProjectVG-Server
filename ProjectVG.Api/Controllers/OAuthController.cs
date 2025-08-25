using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
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
        private readonly HttpClient _httpClient;
        private readonly JwtService _jwtService;
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
            _httpClient = httpClientFactory.CreateClient();
            _jwtService = jwtService;
            _oauth2Service = oauth2Service;
            _authService = authService;
            _oauth2Settings = oauth2Settings.Value;
        }

        [HttpGet("oauth2/authorize")]
        public async Task<IActionResult> OAuth2Authorize(
            [FromQuery] string client_id, 
            [FromQuery] string redirect_uri, 
            [FromQuery] string response_type, 
            [FromQuery] string scope,
            [FromQuery] string state,
            [FromQuery] string code_challenge,
            [FromQuery] string code_challenge_method,
            [FromQuery] string code_verifier,
            [FromQuery] string client_redirect_uri)
        {
            if (string.IsNullOrEmpty(code_challenge) || code_challenge_method != "S256")
            {
                return BadRequest("Invalid PKCE parameters");
            }

            // 설정에서 Google OAuth2 정보 가져오기
            if (!_oauth2Settings.Providers.TryGetValue("google", out var googleProvider) || !googleProvider.Enabled)
            {
                return BadRequest("Google OAuth2 is not configured or disabled");
            }

            // Google OAuth2 설정 확인
            if (string.IsNullOrEmpty(googleProvider.ClientId))
            {
                return BadRequest("Google OAuth2 Client ID is not configured");
            }
            
            // OAuth2 요청 정보 저장
            var authRequest = new OAuth2AuthRequest
            {
                ClientId = googleProvider.ClientId,
                RedirectUri = googleProvider.RedirectUri,
                ClientRedirectUri = client_redirect_uri ?? "http://localhost:3000", // 클라이언트가 제공한 콜백 URL
                State = state,
                CodeChallenge = code_challenge,
                CodeVerifier = code_verifier,
                CodeChallengeMethod = code_challenge_method,
                CreatedAt = DateTime.UtcNow
            };
            
            await _oauth2Service.StoreOAuth2RequestAsync(state, authRequest);
            
            // Google OAuth2 URL 생성
            var googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth" +
                               $"?client_id={Uri.EscapeDataString(googleProvider.ClientId)}" +
                               $"&redirect_uri={Uri.EscapeDataString(googleProvider.RedirectUri)}" +
                               $"&response_type=code" +
                               $"&scope={Uri.EscapeDataString(scope)}" +
                               $"&state={state}" +
                               $"&code_challenge={code_challenge}" +
                               $"&code_challenge_method={code_challenge_method}";
            
            return Ok(new
            {
                success = true,
                auth_url = googleAuthUrl,
                state = state,
                message = "Redirect to this URL to start OAuth2 login"
            });
        }

        [HttpGet("oauth2/callback")]
        [HttpGet("google/callback")]
        public async Task<IActionResult> OAuth2Callback(
            [FromQuery] string code,
            [FromQuery] string state,
            [FromQuery] string error = null)
        {
            try
            {
                // 에러 체크
                if (!string.IsNullOrEmpty(error))
                {
                    return BadRequest(new { success = false, message = $"OAuth2 error: {error}" });
                }

                // 필수 파라미터 체크
                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                {
                    return BadRequest(new { success = false, message = "Missing required parameters: code or state" });
                }

                // 저장된 OAuth2 요청 정보 가져오기
                var authRequest = await _oauth2Service.GetOAuth2RequestAsync(state);
                if (authRequest == null)
                {
                    return BadRequest(new { success = false, message = "Invalid or expired authorization request" });
                }

                // Google Token API 호출 (Authorization Code → Access Token)
                var tokenResponse = await _oauth2Service.ExchangeAuthorizationCodeAsync(
                    code, 
                    authRequest.ClientId, 
                    authRequest.RedirectUri,
                    authRequest.CodeVerifier);

                if (!tokenResponse.Success)
                {
                    return BadRequest(new { success = false, message = tokenResponse.Message });
                }

                // 사용된 authorization request 삭제
                await _oauth2Service.DeleteOAuth2RequestAsync(state);

                // Google Users Info API 호출
                var providerName = GetProviderNameFromClientId(authRequest.ClientId);
                var userInfo = await _oauth2Service.GetUserInfoAsync(tokenResponse.Tokens!.AccessToken, providerName);

                if (string.IsNullOrEmpty(userInfo.Id))
                {
                    return BadRequest(new { success = false, message = "Failed to get user information" });
                }

                // 자체 JWT 발급
                var authResult = await _authService.LoginWithOAuthAsync(providerName, userInfo.Id);

                if (!authResult.IsSuccess)
                {
                    return BadRequest(new { success = false, message = authResult.ErrorMessage });
                }

                // JWT 토큰을 임시 저장 (state를 키로 사용)
                var tokenData = new OAuth2TokenData
                {
                    AccessToken = authResult.Tokens!.AccessToken,
                    RefreshToken = authResult.Tokens.RefreshToken,
                    ExpiresIn = (int)(authResult.Tokens.AccessTokenExpiresAt - DateTime.UtcNow).TotalSeconds,
                    UID = authResult.User!.UID,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _oauth2Service.StoreTokenDataAsync(state, tokenData);

                // 성공 시 클라이언트가 요청한 URL로 리다이렉트 (state만 포함)
                var clientRedirectUrl = $"{authRequest.ClientRedirectUri}?" +
                                       $"success=true&" +
                                       $"state={Uri.EscapeDataString(state)}";

                return Redirect(clientRedirectUrl);
            }
            catch (Exception ex)
            {
                // 에러 시 클라이언트로 에러 정보와 함께 리다이렉트
                var errorRedirectUrl = $"{Request.Query["redirect_uri"].FirstOrDefault() ?? "/"}?" +
                                     $"success=false&" +
                                     $"error={Uri.EscapeDataString(ex.Message)}";
                return Redirect(errorRedirectUrl);
            }
        }

        [HttpGet("oauth2/token")]
        public async Task<IActionResult> GetOAuth2Token([FromQuery] string state)
        {
            try
            {
                if (string.IsNullOrEmpty(state))
                {
                    return BadRequest(new { success = false, message = "State parameter is required" });
                }

                // 저장된 토큰 데이터 가져오기
                var tokenData = await _oauth2Service.GetTokenDataAsync(state) as OAuth2TokenData;
                if (tokenData == null)
                {
                    return BadRequest(new { success = false, message = "Invalid or expired token request" });
                }

                // 토큰 데이터 삭제 (1회 사용)
                await _oauth2Service.DeleteTokenDataAsync(state);

                // HTTP 헤더로 토큰 전달
                Response.Headers.Append("X-Access-Token", tokenData.AccessToken);
                Response.Headers.Append("X-Refresh-Token", tokenData.RefreshToken);
                Response.Headers.Append("X-Expires-In", tokenData.ExpiresIn.ToString());
                Response.Headers.Append("X-Users-UID", tokenData.UID);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            return Convert.ToBase64String(challengeBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private string GetProviderNameFromClientId(string clientId)
        {
            // 설정에서 클라이언트 ID로 프로바이더 찾기
            foreach (var provider in _oauth2Settings.Providers)
            {
                if (provider.Value.ClientId == clientId)
                {
                    return provider.Key;
                }
            }
            return "google"; // 기본값
        }
    }
}
