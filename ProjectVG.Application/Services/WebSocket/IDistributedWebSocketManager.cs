using ProjectVG.Application.Models.WebSocket;

namespace ProjectVG.Application.Services.WebSocket
{
    /// <summary>
    /// 분산 WebSocket 관리자 인터페이스
    /// </summary>
    public interface IDistributedWebSocketManager
    {
        /// <summary>
        /// 분산 환경에서 WebSocket 세션을 연결합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="serverId">현재 서버 ID</param>
        /// <returns>세션 ID</returns>
        Task<string> ConnectAsync(string userId, string serverId);

        /// <summary>
        /// 분산 환경에서 WebSocket 세션을 해제합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        Task DisconnectAsync(string userId);

        /// <summary>
        /// 사용자에게 메시지를 전송합니다 (분산 라우팅 포함)
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="message">WebSocket 메시지</param>
        Task SendAsync(string userId, WebSocketMessage message);

        /// <summary>
        /// 사용자에게 텍스트 메시지를 전송합니다 (분산 라우팅 포함)
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="text">텍스트 메시지</param>
        Task SendTextAsync(string userId, string text);

        /// <summary>
        /// 사용자에게 바이너리 데이터를 전송합니다 (분산 라우팅 포함)
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="data">바이너리 데이터</param>
        Task SendBinaryAsync(string userId, byte[] data);

        /// <summary>
        /// 여러 사용자에게 동시에 메시지를 전송합니다
        /// </summary>
        /// <param name="userIds">사용자 ID 목록</param>
        /// <param name="message">WebSocket 메시지</param>
        Task SendToMultipleAsync(IEnumerable<string> userIds, WebSocketMessage message);

        /// <summary>
        /// 세션이 현재 서버에 로컬로 연결되어 있는지 확인합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>로컬 연결 여부</returns>
        bool IsLocalSession(string userId);

        /// <summary>
        /// 세션이 분산 환경에서 활성화되어 있는지 확인합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>활성화 여부</returns>
        Task<bool> IsSessionActiveAsync(string userId);

        /// <summary>
        /// 현재 서버의 활성 연결 수를 반환합니다
        /// </summary>
        /// <returns>연결 수</returns>
        int GetLocalConnectionCount();

        /// <summary>
        /// 전체 분산 환경의 활성 세션 수를 반환합니다
        /// </summary>
        /// <returns>세션 수</returns>
        Task<int> GetGlobalSessionCountAsync();
    }
}