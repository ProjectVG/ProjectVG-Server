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

        /// <summary>
        /// AuthController의 인스턴스를 생성합니다. 필요한 의존성(IUserService, ILogger&lt;AuthController&gt;)을 주입받습니다.
        /// </summary>
        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// 새 사용자를 등록하고 등록 결과(AuthResponse)를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 요청 본문에서 전달된 RegisterRequest를 사용자 DTO로 변환하여 사용자 생성 서비스를 호출합니다.
        /// 성공하면 생성된 사용자의 Id, Username, Email을 포함한 AuthResponse를 반환합니다.
        /// </remarks>
        /// <param name="request">회원가입 정보가 담긴 요청 모델(요청 본문).</param>
        /// <returns>회원가입 성공 여부와 생성된 사용자 정보를 포함한 AuthResponse를 가진 ActionResult(200 OK).</returns>
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

        /// <summary>
        /// 사용자의 이름으로 인증을 수행하고 인증 결과(AuthResponse)를 반환합니다.
        /// </summary>
        /// <param name="request">로그인에 사용되는 요청 객체(사용자명 및 자격 증명 포함). 이 메서드는 요청의 Username으로 사용자를 조회합니다.</param>
        /// <returns>인증 성공 여부와 사용자 식별 정보(Id, Username, Email)를 담은 <see cref="AuthResponse"/>를 HTTP 200 OK로 반환합니다.</returns>
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

        /// <summary>
        /// 주어진 사용자명(username)이 이미 존재하는지 확인합니다.
        /// </summary>
        /// <param name="username">경로에서 전달된 검사할 사용자명.</param>
        /// <returns>Exists 필드에 존재 여부(true/false)를 담고, 상황에 맞는 한글 메시지를 포함한 CheckResponse를 반환합니다.</returns>
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

        /// <summary>
        — 주어진 이메일의 존재 여부를 확인합니다.
        </summary>
        --- Oops I must follow format: only docstring. I accidentally included extra lines. Need to output only the docstring. Let's correct.
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