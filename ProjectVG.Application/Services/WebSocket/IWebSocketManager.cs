using ProjectVG.Application.Models.WebSocket;

namespace ProjectVG.Application.Services.WebSocket
{
    public interface IWebSocketManager
    {
        /// <summary>
        /// WebSocket 연결을 생성하고 초기화합니다
        /// </summary>
        Task<string> ConnectAsync(string? sessionId = null);
        
        /// <summary>
        /// WebSocket 메시지를 전송합니다
        /// </summary>
        Task SendAsync(string sessionId, WebSocketMessage message);
        
        /// <summary>
        /// 텍스트 메시지를 전송합니다
        /// </summary>
        Task SendTextAsync(string sessionId, string text);
        
        /// <summary>
        /// 바이너리 데이터를 전송합니다
        /// </summary>
        Task SendBinaryAsync(string sessionId, byte[] data);
        
        /// <summary>
        /// 클라이언트 메시지를 처리합니다
        /// </summary>
        Task HandleMessageAsync(string sessionId, string message);
        
        /// <summary>
        /// 바이너리 메시지를 처리합니다
        /// </summary>
        Task HandleBinaryMessageAsync(string sessionId, byte[] data);
        
        /// <summary>
        /// WebSocket 연결을 종료합니다
        /// </summary>
        Task DisconnectAsync(string sessionId);
        
        /// <summary>
        /// 세션이 활성 상태인지 확인합니다
        /// </summary>
        bool IsSessionActive(string sessionId);
    }
}
