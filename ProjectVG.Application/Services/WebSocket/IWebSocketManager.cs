using ProjectVG.Application.Models.WebSocket;

namespace ProjectVG.Application.Services.WebSocket
{
    public interface IWebSocketManager
    {
        /// <summary>
        /// WebSocket 연결을 생성하고 초기화합니다
        /// <summary>
/// 지정한 사용자(userId)에 대한 WebSocket 연결을 비동기적으로 생성하고 초기화합니다.
/// </summary>
/// <param name="userId">연결을 생성할 대상 사용자 식별자(비어있거나 null일 수 없습니다).</param>
/// <returns>생성된 연결의 식별자(연결 ID). 이후 SendAsync, DisconnectAsync, IsSessionActive 등의 호출에 사용됩니다.</returns>
        Task<string> ConnectAsync(string userId);
        
        /// <summary>
        /// WebSocket 메시지를 전송합니다
        /// <summary>
/// 지정한 사용자의 WebSocket 연결로 WebSocketMessage를 비동기 전송합니다.
/// </summary>
/// <param name="userId">메시지를 수신할 대상 사용자의 식별자.</param>
/// <param name="message">전송할 WebSocketMessage 객체.</param>
/// <returns>전송 작업이 완료될 때까지 대기하는 비동기 작업.</returns>
        Task SendAsync(string userId, WebSocketMessage message);
        
        /// <summary>
        /// WebSocket 연결을 종료합니다
        /// <summary>
/// 지정된 사용자(userId)에 대한 WebSocket 연결을 비동기적으로 종료합니다.
/// </summary>
/// <param name="userId">연결을 종료할 대상 사용자의 고유 식별자(널이 아닌 값).</param>
/// <returns>연결 해제가 완료될 때까지 대기하는 Task.</returns>
        Task DisconnectAsync(string userId);
        
        /// <summary>
        /// 세션이 활성 상태인지 확인합니다
        /// <summary>
/// 지정한 사용자(userId)에 대한 WebSocket 세션이 현재 활성 상태인지 확인합니다.
/// </summary>
/// <param name="userId">활성 상태를 확인할 대상 사용자 식별자.</param>
/// <returns>세션이 활성화되어 있으면 true, 그렇지 않으면 false.</returns>
        bool IsSessionActive(string userId);
    }
}
