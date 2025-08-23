namespace ProjectVG.Infrastructure.Auth.Models;

public class KeyMetadata
{
    public string CurrentKeyId { get; set; } = string.Empty;
    public List<KeyInfo> Keys { get; set; } = new();
}

public class KeyInfo
{
    public string KeyId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "RS256";
}
