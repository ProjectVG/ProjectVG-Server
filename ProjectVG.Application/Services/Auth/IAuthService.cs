using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Application.Services.Auth
{
    public interface IAuthService
    {
        /// <summary>
        /// 사용자 로그인 처리
        /// </summary>
        /// <param name="request">로그인 요청</param>
        /// <param name="clientIP">클라이언트 IP</param>
        /// <param name="clientPort">클라이언트 Port</param>
        /// <returns>로그인 결과</returns>
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string clientIP, int clientPort);

        /// <summary>
        /// 사용자 회원가입 처리
        /// </summary>
        /// <param name="request">회원가입 요청</param>
        /// <returns>회원가입 결과</returns>
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    }
} 