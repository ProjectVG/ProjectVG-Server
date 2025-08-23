namespace ProjectVG.Infrastructure.Cache;

public interface ITokenBlocklistService
{
    Task BlockRefreshTokenAsync(string tokenHash, TimeSpan expiry);
    Task<bool> IsRefreshTokenBlockedAsync(string tokenHash);
    Task BlockAccessTokenAsync(string jti, TimeSpan expiry);
    Task<bool> IsAccessTokenBlockedAsync(string jti);
    Task UnblockRefreshTokenAsync(string tokenHash);
    Task UnblockAccessTokenAsync(string jti);
    Task CleanupExpiredBlocksAsync();
    Task<long> GetBlockedTokenCountAsync();
}
