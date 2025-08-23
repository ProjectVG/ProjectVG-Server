using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Models.Auth;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Application.Services.User;

namespace ProjectVG.Api.Controllers;

[ApiController]
[Route("oauth2")]
public class OAuth2Controller : ControllerBase
{
    private readonly IOAuth2Service _oauth2Service;
    private readonly ITokenService _tokenService;
    private readonly IUserService _userService;
    private readonly ILogger<OAuth2Controller> _logger;

    public OAuth2Controller(
        IOAuth2Service oauth2Service,
        ITokenService tokenService,
        IUserService userService,
        ILogger<OAuth2Controller> logger)
    {
        _oauth2Service = oauth2Service;
        _tokenService = tokenService;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize([FromQuery] OAuth2AuthorizeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new OAuth2ErrorResponse
            {
                Error = "invalid_request",
                ErrorDescription = "The request is missing required parameters or contains invalid values.",
                State = request.State
            });
        }

        // 응답 타입 검증
        if (request.ResponseType != "code")
        {
            return BadRequest(new OAuth2ErrorResponse
            {
                Error = "unsupported_response_type",
                ErrorDescription = "The authorization server does not support obtaining an authorization code using this method.",
                State = request.State
            });
        }

        // 클라이언트 검증
        if (!await _oauth2Service.ValidateClientAsync(request.ClientId))
        {
            return BadRequest(new OAuth2ErrorResponse
            {
                Error = "invalid_client",
                ErrorDescription = "Client authentication failed.",
                State = request.State
            });
        }

        // Redirect URI 검증
        if (!await _oauth2Service.ValidateRedirectUriAsync(request.ClientId, request.RedirectUri))
        {
            return BadRequest(new OAuth2ErrorResponse
            {
                Error = "invalid_request",
                ErrorDescription = "The redirect URI is not registered for this client.",
                State = request.State
            });
        }

        // PKCE 매개변수 검증
        if (string.IsNullOrEmpty(request.CodeChallenge) || request.CodeChallengeMethod != "S256")
        {
            return BadRequest(new OAuth2ErrorResponse
            {
                Error = "invalid_request",
                ErrorDescription = "PKCE parameters are required and must use S256 method.",
                State = request.State
            });
        }

        // 임시: 현재 인증된 사용자가 없으므로 테스트용 사용자 ID 사용
        // 실제 구현에서는 로그인 페이지로 리다이렉트하거나 현재 인증된 사용자를 사용
        var userId = Guid.Parse("12345678-1234-1234-1234-123456789012"); // 테스트용

        try
        {
            // Authorization Code 생성
            var code = await _oauth2Service.GenerateAuthorizationCodeAsync(
                request.ClientId,
                userId,
                request.CodeChallenge,
                request.CodeChallengeMethod,
                request.RedirectUri,
                request.Scope,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()
            );

            var redirectUrl = string.IsNullOrEmpty(request.RedirectUri)
                ? $"unity://oauth2/callback?code={code}&state={request.State}"
                : $"{request.RedirectUri}?code={code}&state={request.State}";

            _logger.LogInformation("Generated authorization code for client {ClientId}", request.ClientId);

            return Ok(new
            {
                redirect_url = redirectUrl,
                code,
                state = request.State
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating authorization code");
            return BadRequest(new OAuth2ErrorResponse
            {
                Error = "server_error",
                ErrorDescription = "The authorization server encountered an unexpected condition.",
                State = request.State
            });
        }
    }

    [HttpPost("token")]
    public async Task<IActionResult> Token([FromForm] OAuth2TokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new OAuth2ErrorResponse
            {
                Error = "invalid_request",
                ErrorDescription = "The request is missing required parameters or contains invalid values."
            });
        }

        // Grant Type 검증
        if (request.GrantType != "authorization_code")
        {
            return BadRequest(new OAuth2ErrorResponse
            {
                Error = "unsupported_grant_type",
                ErrorDescription = "The authorization grant type is not supported by the authorization server."
            });
        }

        // 클라이언트 검증
        if (!await _oauth2Service.ValidateClientAsync(request.ClientId))
        {
            return BadRequest(new OAuth2ErrorResponse
            {
                Error = "invalid_client",
                ErrorDescription = "Client authentication failed."
            });
        }

        try
        {
            // Authorization Code 교환
            var authCode = await _oauth2Service.ExchangeCodeAsync(
                request.Code, 
                request.ClientId, 
                request.CodeVerifier, 
                request.RedirectUri);

            if (authCode == null)
            {
                return BadRequest(new OAuth2ErrorResponse
                {
                    Error = "invalid_grant",
                    ErrorDescription = "The provided authorization grant is invalid, expired, or revoked."
                });
            }

            // 토큰 생성
            var tokenResponse = await _tokenService.GenerateTokensAsync(
                authCode.UserId,
                request.ClientId,
                deviceId: null, // 필요시 클라이언트에서 제공
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()
            );

            _logger.LogInformation("Issued tokens for user {UserId}, client {ClientId}", 
                authCode.UserId, request.ClientId);

            return Ok(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing token request");
            return BadRequest(new OAuth2ErrorResponse
            {
                Error = "server_error",
                ErrorDescription = "The authorization server encountered an unexpected condition."
            });
        }
    }

    [HttpGet("authorize-test")]
    [AllowAnonymous]
    public IActionResult AuthorizeTest()
    {
        return Ok(new
        {
            message = "OAuth2 Authorization endpoint is working",
            sample_request = new
            {
                response_type = "code",
                client_id = "unity-client",
                redirect_uri = "http://localhost:3000/callback",
                scope = "openid profile",
                state = "random-state-value",
                code_challenge = "sample-code-challenge",
                code_challenge_method = "S256"
            }
        });
    }
}
