using ProjectVG.Application.Models.User;
using ProjectVG.Common.Models;

namespace ProjectVG.Application.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResult> LoginWithOAuthAsync(string provider, string providerUserId);
        Task<AuthResult> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(string refreshToken);
        Task<bool> ValidateAccessTokenAsync(string accessToken);
        Task<Guid?> GetCurrentUserIdAsync(string accessToken);
    }

    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public TokenResponse? Tokens { get; set; }
        public UserDto? User { get; set; }
    }
}
