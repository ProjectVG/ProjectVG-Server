using ProjectVG.Domain.Common;

namespace ProjectVG.Domain.Entities.Auth;

public class RefreshToken : BaseEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public DateTime LastUsedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;

    public RefreshToken? ReplacedByToken { get; set; }
    public ICollection<RefreshToken> ReplacedTokens { get; set; } = new List<RefreshToken>();
}
