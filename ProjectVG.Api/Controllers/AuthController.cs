using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Services.User;
using ProjectVG.Application.Models.User;
using ProjectVG.Api.Models.Auth.Request;
using ProjectVG.Api.Models.Auth.Response;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var userDto = request.ToUserDto();
            var createdUser = await _userService.CreateUserAsync(userDto);
            
            var response = new AuthResponse
            {
                Success = true,
                Message = "회원가입이 완료되었습니다.",
                UserId = createdUser.Id,
                Username = createdUser.Username,
                Email = createdUser.Email
            };

            _logger.LogInformation("새 사용자 회원가입 완료: {Username}", createdUser.Username);
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var user = await _userService.GetUserByUsernameAsync(request.Username);
            var response = new AuthResponse
            {
                Success = true,
                Message = "로그인이 완료되었습니다.",
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email
            };

            _logger.LogInformation("사용자 로그인 완료: {Username}", user.Username);
            return Ok(response);
        }

        [HttpGet("check-username/{username}")]
        public async Task<ActionResult<CheckResponse>> CheckUsername(string username)
        {
            var exists = await _userService.UsernameExistsAsync(username);
            return Ok(new CheckResponse
            {
                Exists = exists,
                Message = exists ? "이미 사용 중인 사용자명입니다." : "사용 가능한 사용자명입니다."
            });
        }

        [HttpGet("check-email/{email}")]
        public async Task<ActionResult<CheckResponse>> CheckEmail(string email)
        {
            var exists = await _userService.EmailExistsAsync(email);
            return Ok(new CheckResponse
            {
                Exists = exists,
                Message = exists ? "이미 사용 중인 이메일입니다." : "사용 가능한 이메일입니다."
            });
        }
    }
} 