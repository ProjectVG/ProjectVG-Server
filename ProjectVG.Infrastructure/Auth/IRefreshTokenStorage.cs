namespace ProjectVG.Infrastructure.Auth
{
    public interface IRefreshTokenStorage
    {
        Task<bool> StoreRefreshTokenAsync(string refreshToken, Guid userId, DateTime expiresAt);
        Task<Guid?> GetUserIdFromRefreshTokenAsync(string refreshToken);
        Task<bool> RemoveRefreshTokenAsync(string refreshToken);
        Task<bool> IsRefreshTokenValidAsync(string refreshToken);
    }
}
