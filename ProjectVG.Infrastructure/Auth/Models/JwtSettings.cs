namespace ProjectVG.Infrastructure.Auth.Models;

public class JwtSettings
{
    public const string SectionName = "Authentication:Jwt";
    
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string KeyStore { get; set; } = "FileSystem";
    public string? KeysDirectory { get; set; }
    public string? KeyVaultUrl { get; set; }
    public int RotationIntervalDays { get; set; } = 90;
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}
