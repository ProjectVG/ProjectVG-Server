namespace ProjectVG.Application.Models.Server
{
    public class ServerInfo
    {
        public string ServerId { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public int ActiveConnections { get; set; }
        public string Status { get; set; } = "healthy";
        public string? Environment { get; set; }
        public string? Version { get; set; }

        public ServerInfo()
        {
        }

        public ServerInfo(string serverId)
        {
            ServerId = serverId;
            StartedAt = DateTime.UtcNow;
            LastHeartbeat = DateTime.UtcNow;
            ActiveConnections = 0;
            Status = "healthy";
        }

        public void UpdateHeartbeat()
        {
            LastHeartbeat = DateTime.UtcNow;
        }

        public void UpdateConnectionCount(int count)
        {
            ActiveConnections = count;
        }

        public bool IsHealthy(TimeSpan timeout)
        {
            return DateTime.UtcNow - LastHeartbeat < timeout;
        }
    }
}