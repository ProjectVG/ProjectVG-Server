using ProjectVG.Application.Models.WebSocket;

namespace ProjectVG.Application.Services.WebSocket
{
    public interface IWebSocketManager
    {
        /// <summary>
        /// WebSocket 연결을 생성하고 초기화합니다
        /// </summary>
        Task<string> ConnectAsync(string userId);

        /// <summary>
        /// WebSocket 메시지를 전송합니다
        /// </summary>
        Task SendAsync(string userId, WebSocketMessage message);

        /// <summary>
        /// WebSocket 연결을 종료합니다
        /// </summary>
        Task DisconnectAsync(string userId);

        /// <summary>
        /// 세션이 활성 상태인지 확인합니다
        /// </summary>
        bool IsSessionActive(string userId);

        /// <summary>
        /// 세션 하트비트를 업데이트합니다 (Redis TTL 갱신)
        /// </summary>
        Task UpdateSessionHeartbeatAsync(string userId);
    }
}
