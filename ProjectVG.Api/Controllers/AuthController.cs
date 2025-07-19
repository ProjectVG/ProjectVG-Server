using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Application.Models.Auth;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// 회원가입
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(request);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }

        /// <summary>
        /// 로그인
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 클라이언트 IP와 포트 가져오기
            var clientIP = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var clientPort = HttpContext.Connection.RemotePort;

            var result = await _authService.LoginAsync(request, clientIP, clientPort);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return Unauthorized(result);
            }
        }
    }
} 