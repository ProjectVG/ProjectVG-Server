using ProjectVG.Application.Services.User;
using ProjectVG.Application.Services.Session;
using ProjectVG.Application.Models.User;
using ProjectVG.Application.Models.Auth;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly IClientSessionService _clientSessionService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserService userService, 
            IClientSessionService clientSessionService, 
            ILogger<AuthService> logger)
        {
            _userService = userService;
            _clientSessionService = clientSessionService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string clientIP, int clientPort)
        {
            try
            {
                // 항상 Test User 사용 (ID: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa)
                var testUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
                var user = await _userService.GetUserByIdAsync(testUserId);
                
                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Test User를 찾을 수 없습니다."
                    };
                }

                // 세션 생성
                var session = await _clientSessionService.CreateSessionAsync(user.Id, clientIP, clientPort);

                var response = new AuthResponseDto
                {
                    Success = true,
                    Message = "로그인이 완료되었습니다.",
                    UserId = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    SessionId = session.SessionId
                };

                _logger.LogInformation("Test User 로그인 완료: {Username}, 세션 ID: {SessionId}", user.Username, session.SessionId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "로그인 중 오류가 발생했습니다");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "로그인 중 내부 서버 오류가 발생했습니다."
                };
            }
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            try
            {
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
                return response;
            }
            catch (InvalidOperationException ex)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "회원가입 중 오류가 발생했습니다");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "회원가입 중 내부 서버 오류가 발생했습니다."
                };
            }
        }
    }
} 