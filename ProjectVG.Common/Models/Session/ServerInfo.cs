namespace ProjectVG.Common.Models.Session
{
    /// <summary>
    /// 서버 정보를 나타내는 모델
    /// </summary>
    public class ServerInfo
    {
        public string ServerId { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public string Version { get; set; } = string.Empty;
        public ServerStatus Status { get; set; } = ServerStatus.Online;
        public int ActiveConnections { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();

        public ServerInfo()
        {
            RegisteredAt = DateTime.UtcNow;
            LastHeartbeat = DateTime.UtcNow;
        }

        public ServerInfo(string serverId, string hostName, string ipAddress, int port)
        {
            ServerId = serverId;
            HostName = hostName;
            IpAddress = ipAddress;
            Port = port;
            RegisteredAt = DateTime.UtcNow;
            LastHeartbeat = DateTime.UtcNow;
        }

        /// <summary>
        /// 서버가 온라인 상태인지 확인
        /// </summary>
        /// <param name="heartbeatTimeout">하트비트 타임아웃 (기본 30초)</param>
        /// <returns>온라인 여부</returns>
        public bool IsOnline(TimeSpan? heartbeatTimeout = null)
        {
            var timeout = heartbeatTimeout ?? TimeSpan.FromSeconds(30);
            return Status == ServerStatus.Online &&
                   DateTime.UtcNow - LastHeartbeat <= timeout;
        }

        /// <summary>
        /// 하트비트 업데이트
        /// </summary>
        public void UpdateHeartbeat()
        {
            LastHeartbeat = DateTime.UtcNow;
            Status = ServerStatus.Online;
        }
    }

    public enum ServerStatus
    {
        Online,
        Offline,
        Maintenance,
        Unhealthy
    }
}