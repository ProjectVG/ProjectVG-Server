using Microsoft.EntityFrameworkCore;
using ProjectVG.Domain.Entities.Auth;
using ProjectVG.Infrastructure.Persistence.EfCore;

namespace ProjectVG.Infrastructure.Persistence.Repositories.Auth;

public class SqlServerRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ProjectVGDbContext _context;

    public SqlServerRefreshTokenRepository(ProjectVGDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken?> GetByIdAsync(Guid id)
    {
        return await _context.RefreshTokens.FindAsync(id);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public async Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId)
    {
        return await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<RefreshToken>> GetTokenFamilyAsync(Guid tokenId)
    {
        var allTokens = await _context.RefreshTokens.ToListAsync();
        var family = new HashSet<RefreshToken>();
        var visited = new HashSet<Guid>();

        await CollectTokenFamilyRecursive(tokenId, allTokens, family, visited);
        return family;
    }

    public async Task<IEnumerable<RefreshToken>> GetExpiredTokensAsync(DateTime cutoffDate)
    {
        return await _context.RefreshTokens
            .Where(t => t.ExpiresAt < cutoffDate)
            .ToListAsync();
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return refreshToken;
    }

    public async Task UpdateAsync(RefreshToken refreshToken)
    {
        refreshToken.Update();
        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var token = await GetByIdAsync(id);
        if (token != null)
        {
            _context.RefreshTokens.Remove(token);
            await _context.SaveChangesAsync();
        }
    }

    private async Task CollectTokenFamilyRecursive(Guid tokenId, List<RefreshToken> allTokens, HashSet<RefreshToken> family, HashSet<Guid> visited)
    {
        if (visited.Contains(tokenId))
            return;

        visited.Add(tokenId);
        var token = allTokens.FirstOrDefault(t => t.Id == tokenId);
        
        if (token == null)
            return;

        family.Add(token);

        // 이 토큰으로 교체된 토큰들 찾기
        var replacedTokens = allTokens.Where(t => t.ReplacedByTokenId == tokenId).ToList();
        foreach (var replacedToken in replacedTokens)
        {
            await CollectTokenFamilyRecursive(replacedToken.Id, allTokens, family, visited);
        }

        // 이 토큰을 교체한 토큰 찾기
        if (token.ReplacedByTokenId.HasValue)
        {
            await CollectTokenFamilyRecursive(token.ReplacedByTokenId.Value, allTokens, family, visited);
        }
    }
}
