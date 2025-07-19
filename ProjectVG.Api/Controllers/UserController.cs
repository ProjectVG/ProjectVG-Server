using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Services.User;
using ProjectVG.Domain.Entities.User;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// 모든 사용자 조회
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "모든 사용자 조회 중 오류가 발생했습니다");
                return StatusCode(500, "사용자 목록을 가져오는 중 내부 서버 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// ID로 사용자 조회
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUserById(Guid id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"ID {id}인 사용자를 찾을 수 없습니다.");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ID {UserId}인 사용자 조회 중 오류가 발생했습니다", id);
                return StatusCode(500, "사용자 정보를 가져오는 중 내부 서버 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// 이메일로 사용자 조회
        /// </summary>
        [HttpGet("email/{email}")]
        public async Task<ActionResult<User>> GetUserByEmail(string email)
        {
            try
            {
                var user = await _userService.GetUserByEmailAsync(email);
                if (user == null)
                {
                    return NotFound($"이메일 {email}인 사용자를 찾을 수 없습니다.");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "이메일 {Email}인 사용자 조회 중 오류가 발생했습니다", email);
                return StatusCode(500, "사용자 정보를 가져오는 중 내부 서버 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// 사용자명으로 사용자 조회
        /// </summary>
        [HttpGet("username/{username}")]
        public async Task<ActionResult<User>> GetUserByUsername(string username)
        {
            try
            {
                var user = await _userService.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    return NotFound($"사용자명 {username}인 사용자를 찾을 수 없습니다.");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자명 {Username}인 사용자 조회 중 오류가 발생했습니다", username);
                return StatusCode(500, "사용자 정보를 가져오는 중 내부 서버 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// Provider ID로 사용자 조회
        /// </summary>
        [HttpGet("provider/{providerId}")]
        public async Task<ActionResult<User>> GetUserByProviderId(string providerId)
        {
            try
            {
                var user = await _userService.GetUserByProviderIdAsync(providerId);
                if (user == null)
                {
                    return NotFound($"Provider ID {providerId}인 사용자를 찾을 수 없습니다.");
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider ID {ProviderId}인 사용자 조회 중 오류가 발생했습니다", providerId);
                return StatusCode(500, "사용자 정보를 가져오는 중 내부 서버 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// 새 사용자 생성
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] User user)
        {
            try
            {
                var createdUser = await _userService.CreateUserAsync(user);
                return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 생성 중 오류가 발생했습니다");
                return StatusCode(500, "사용자 생성 중 내부 서버 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// 사용자 정보 수정
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<User>> UpdateUser(Guid id, [FromBody] User user)
        {
            try
            {
                if (id != user.Id)
                {
                    return BadRequest("URL의 ID와 요청 본문의 ID가 일치하지 않습니다.");
                }

                var updatedUser = await _userService.UpdateUserAsync(user);
                return Ok(updatedUser);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ID {UserId}인 사용자 수정 중 오류가 발생했습니다", id);
                return StatusCode(500, "사용자 정보 수정 중 내부 서버 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// 사용자 삭제
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(Guid id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ID {UserId}인 사용자 삭제 중 오류가 발생했습니다", id);
                return StatusCode(500, "사용자 삭제 중 내부 서버 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// 이메일 중복 확인
        /// </summary>
        [HttpGet("exists/email/{email}")]
        public async Task<ActionResult<bool>> CheckEmailExists(string email)
        {
            try
            {
                var exists = await _userService.EmailExistsAsync(email);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "이메일 {Email} 존재 여부 확인 중 오류가 발생했습니다", email);
                return StatusCode(500, "이메일 중복 확인 중 내부 서버 오류가 발생했습니다.");
            }
        }

        /// <summary>
        /// 사용자명 중복 확인
        /// </summary>
        [HttpGet("exists/username/{username}")]
        public async Task<ActionResult<bool>> CheckUsernameExists(string username)
        {
            try
            {
                var exists = await _userService.UsernameExistsAsync(username);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자명 {Username} 존재 여부 확인 중 오류가 발생했습니다", username);
                return StatusCode(500, "사용자명 중복 확인 중 내부 서버 오류가 발생했습니다.");
            }
        }
    }
} 