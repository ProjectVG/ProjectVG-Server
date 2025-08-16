using ProjectVG.Application.Models.WebSocket;

namespace ProjectVG.Application.Services.WebSocket
{
    public interface IWebSocketManager
    {
        /// <summary>
        /// WebSocket 연결을 생성하고 초기화합니다
        /// <summary>
/// WebSocket 세션을 생성하거나 초기화하고 세션 식별자를 반환합니다.
/// </summary>
/// <param name="sessionId">기존 세션을 재사용하려는 경우 전달하는 세션 ID. null이면 새 세션을 생성합니다.</param>
/// <returns>생성되었거나 연결된 세션의 식별자 문자열을 비동기적으로 반환합니다.</returns>
        Task<string> ConnectAsync(string? sessionId = null);
        
        /// <summary>
        /// WebSocket 메시지를 전송합니다
        /// <summary>
/// 지정한 세션으로 WebSocketMessage를 비동기적으로 전송합니다.
/// </summary>
/// <param name="sessionId">메시지를 받을 대상 세션의 식별자.</param>
/// <param name="message">전송할 WebSocketMessage 객체.</param>
/// <returns>전송 작업이 완료될 때까지 대기하는 Task.</returns>
        Task SendAsync(string sessionId, WebSocketMessage message);
        
        /// <summary>
        /// 텍스트 메시지를 전송합니다
        /// <summary>
/// 지정된 세션으로 텍스트 메시지를 비동기로 전송합니다.
/// </summary>
/// <param name="sessionId">메시지를 받을 대상 세션의 식별자.</param>
/// <param name="text">전송할 텍스트 페이로드.</param>
        Task SendTextAsync(string sessionId, string text);
        
        /// <summary>
        /// 바이너리 데이터를 전송합니다
        /// <summary>
/// 지정한 세션으로 바이너리 데이터를 비동기 전송합니다.
/// </summary>
/// <param name="sessionId">데이터를 전송할 대상 세션의 식별자.</param>
/// <param name="data">전송할 바이너리 페이로드.</param>
/// <returns>전송 작업이 완료될 때까지의 비동기 작업(Task).</returns>
        Task SendBinaryAsync(string sessionId, byte[] data);
        
        /// <summary>
        /// 클라이언트 메시지를 처리합니다
        /// <summary>
/// 지정된 세션으로부터 수신된 텍스트 메시지를 비동기적으로 처리합니다.
/// </summary>
/// <param name="sessionId">메시지를 보낸 또는 처리 대상이 되는 세션의 식별자.</param>
/// <param name="message">수신된 텍스트 메시지 페이로드(애플리케이션별 파싱/처리 필요).</param>
/// <returns>메시지 처리가 완료될 때까지 대기하는 작업을 나타내는 <see cref="Task"/>.</returns>
        Task HandleMessageAsync(string sessionId, string message);
        
        /// <summary>
        /// 바이너리 메시지를 처리합니다
        /// <summary>
/// 지정된 세션에서 수신된 바이너리 메시지를 비동기적으로 처리합니다.
/// </summary>
/// <param name="sessionId">메시지를 보낸 세션의 식별자(활성 세션 ID).</param>
/// <param name="data">수신된 원시 바이너리 페이로드.</param>
        Task HandleBinaryMessageAsync(string sessionId, byte[] data);
        
        /// <summary>
        /// WebSocket 연결을 종료합니다
        /// <summary>
/// 지정된 세션의 WebSocket 연결을 종료합니다.
/// </summary>
/// <param name="sessionId">종료할 세션의 식별자.</param>
/// <returns>연결 종료가 완료될 때까지 완료되는 비동기 작업.</returns>
        Task DisconnectAsync(string sessionId);
        
        /// <summary>
        /// 세션이 활성 상태인지 확인합니다
        /// <summary>
/// Checks whether the specified WebSocket session is currently active.
/// </summary>
/// <param name="sessionId">The unique identifier of the WebSocket session.</param>
/// <returns>True if the session is active, false otherwise.</returns>
        bool IsSessionActive(string sessionId);
    }
}
