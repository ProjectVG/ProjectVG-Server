using ProjectVG.Common.Models;

namespace ProjectVG.Infrastructure.Auth
{
    public interface ITokenService
    {
        Task<TokenResponse> GenerateTokensAsync(Guid userId);
        Task<TokenResponse?> RefreshAccessTokenAsync(string refreshToken);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);
        Task<bool> ValidateRefreshTokenAsync(string refreshToken);
        Task<Guid?> GetUserIdFromTokenAsync(string token);
    }
}
