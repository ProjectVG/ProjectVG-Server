using ProjectVG.Domain.Entities.Auth;

namespace ProjectVG.Infrastructure.Persistence.Repositories.Auth;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByIdAsync(Guid id);
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId);
    Task<IEnumerable<RefreshToken>> GetTokenFamilyAsync(Guid tokenId);
    Task<IEnumerable<RefreshToken>> GetExpiredTokensAsync(DateTime cutoffDate);
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
    Task UpdateAsync(RefreshToken refreshToken);
    Task DeleteAsync(Guid id);
}
