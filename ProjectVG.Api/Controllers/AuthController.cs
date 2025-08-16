using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Services.User;
using ProjectVG.Application.Models.User;

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

        /// <summary>
        /// 회원가입
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userDto = new UserDto
                {
                    Username = request.Username,
                    Name = request.Name,
                    Email = request.Email,
                    Provider = "local",
                    ProviderId = request.Username,
                    IsActive = true
                };

                var createdUser = await _userService.CreateUserAsync(userDto);
                
                var response = new AuthResponseDto
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new AuthResponseDto
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "회원가입 중 오류가 발생했습니다");
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "회원가입 중 내부 서버 오류가 발생했습니다."
                });
            }
        }

        /// <summary>
        /// 로그인
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // 사용자명으로 사용자 조회
                var user = await _userService.GetUserByUsernameAsync(request.Username);
                if (user == null)
                {
                    return Unauthorized(new AuthResponseDto
                    {
                        Success = false,
                        Message = "사용자명 또는 비밀번호가 올바르지 않습니다."
                    });
                }

                // TODO: 실제 비밀번호 검증 로직 추가 필요

                var response = new AuthResponseDto
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "로그인 중 오류가 발생했습니다");
                return StatusCode(500, new AuthResponseDto
                {
                    Success = false,
                    Message = "로그인 중 내부 서버 오류가 발생했습니다."
                });
            }
        }

        /// <summary>
        /// 사용자명 중복 확인
        /// </summary>
        [HttpGet("check-username/{username}")]
        public async Task<ActionResult<CheckResponseDto>> CheckUsername(string username)
        {
            try
            {
                var exists = await _userService.UsernameExistsAsync(username);
                return Ok(new CheckResponseDto
                {
                    Exists = exists,
                    Message = exists ? "이미 사용 중인 사용자명입니다." : "사용 가능한 사용자명입니다."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자명 중복 확인 중 오류가 발생했습니다");
                return StatusCode(500, new CheckResponseDto
                {
                    Exists = false,
                    Message = "중복 확인 중 내부 서버 오류가 발생했습니다."
                });
            }
        }

        /// <summary>
        /// 이메일 중복 확인
        /// </summary>
        [HttpGet("check-email/{email}")]
        public async Task<ActionResult<CheckResponseDto>> CheckEmail(string email)
        {
            try
            {
                var exists = await _userService.EmailExistsAsync(email);
                return Ok(new CheckResponseDto
                {
                    Exists = exists,
                    Message = exists ? "이미 사용 중인 이메일입니다." : "사용 가능한 이메일입니다."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "이메일 중복 확인 중 오류가 발생했습니다");
                return StatusCode(500, new CheckResponseDto
                {
                    Exists = false,
                    Message = "중복 확인 중 내부 서버 오류가 발생했습니다."
                });
            }
        }
    }

    // DTO 클래스들
    public class RegisterRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
    }

    public class CheckResponseDto
    {
        public bool Exists { get; set; }
        public string Message { get; set; } = string.Empty;
    }
} 