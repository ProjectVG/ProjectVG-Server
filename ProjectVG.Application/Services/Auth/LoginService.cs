using ProjectVG.Application.Services.Users;
using ProjectVG.Infrastructure.Auth;

namespace ProjectVG.Application.Services.Auth
{
    /// <summary>
    /// 실재 로그인이 이루저지는 서비스
    /// </summary>
    internal class LoginService : ILoginService
    {

        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;

        public LoginService(IUserService userService, ITokenService tokenService) {
            _tokenService = tokenService;
            _userService = userService;
        }

        public void Login(LoginRequest request)
        {
            // 로그인 수행 (예: 사용자 인증, 토큰 생성 등)





            throw new NotImplementedException();
        }
        public void Logout()
        {
            throw new NotImplementedException();
        }
        public void Signup()
        {
            throw new NotImplementedException();
        }
    }
}
