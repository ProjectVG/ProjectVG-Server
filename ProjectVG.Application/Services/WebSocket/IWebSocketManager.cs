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
        Task<bool> IsSessionActiveAsync(string userId);

        /// <summary>
        /// 텍스트 메시지를 전송합니다
        /// </summary>
        Task SendTextAsync(string userId, string text);

        /// <summary>
        /// 바이너리 데이터를 전송합니다
        /// </summary>
        Task SendBinaryAsync(string userId, byte[] data);
    }
}
