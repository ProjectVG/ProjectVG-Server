using System.Net.WebSockets;

namespace ProjectVG.Infrastructure.Services.Session
{
    /// <summary>
    /// 클라이언트 WebSocket 연결 정보
    /// </summary>
    public class ClientConnection
    {
        public string SessionId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public WebSocket Socket { get; set; } = null!;
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    }
} 