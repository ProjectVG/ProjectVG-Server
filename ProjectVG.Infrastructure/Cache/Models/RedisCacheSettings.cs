namespace ProjectVG.Infrastructure.Cache;

public class RedisCacheSettings
{
    public const string SectionName = "Redis";
    
    public string ConnectionString { get; set; } = "localhost:6379";
    public int Database { get; set; } = 0;
    public string? KeyPrefix { get; set; } = "ProjectVG";
    public int ConnectRetry { get; set; } = 3;
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 5000;
}
