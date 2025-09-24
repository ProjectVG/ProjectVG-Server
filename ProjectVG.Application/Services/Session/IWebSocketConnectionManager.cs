using ProjectVG.Common.Models.Session;

namespace ProjectVG.Application.Services.Session
{
    /// <summary>
    /// WebSocket 연결 관리 인터페이스 - 로컬 WebSocket 연결 객체 관리만 담당
    /// </summary>
    public interface IWebSocketConnectionManager
    {
        /// <summary>
        /// WebSocket 연결을 등록합니다
        /// </summary>
        /// <param name="sessionId">세션 ID (사용자 ID)</param>
        /// <param name="connection">WebSocket 연결 객체</param>
        void RegisterConnection(string sessionId, IClientConnection connection);

        /// <summary>
        /// WebSocket 연결을 해제합니다
        /// </summary>
        /// <param name="sessionId">세션 ID (사용자 ID)</param>
        void UnregisterConnection(string sessionId);

        /// <summary>
        /// 로컬에 WebSocket 연결이 있는지 확인합니다
        /// </summary>
        /// <param name="sessionId">세션 ID (사용자 ID)</param>
        /// <returns>로컬 연결 존재 여부</returns>
        bool HasLocalConnection(string sessionId);

        /// <summary>
        /// 특정 세션에 텍스트 메시지를 전송합니다
        /// </summary>
        /// <param name="sessionId">세션 ID (사용자 ID)</param>
        /// <param name="message">전송할 메시지</param>
        /// <returns>전송 성공 여부</returns>
        Task<bool> SendTextAsync(string sessionId, string message);

        /// <summary>
        /// 특정 세션에 바이너리 데이터를 전송합니다
        /// </summary>
        /// <param name="sessionId">세션 ID (사용자 ID)</param>
        /// <param name="data">전송할 바이너리 데이터</param>
        /// <returns>전송 성공 여부</returns>
        Task<bool> SendBinaryAsync(string sessionId, byte[] data);

        /// <summary>
        /// 현재 서버의 로컬 연결 수를 조회합니다
        /// </summary>
        /// <returns>로컬 연결 수</returns>
        int GetLocalConnectionCount();

        /// <summary>
        /// 현재 서버의 모든 로컬 연결된 세션 ID 목록을 조회합니다
        /// </summary>
        /// <returns>로컬 연결된 세션 ID 목록</returns>
        IEnumerable<string> GetLocalConnectedSessionIds();

        /// <summary>
        /// 특정 세션의 WebSocket 연결 객체를 조회합니다
        /// </summary>
        /// <param name="sessionId">세션 ID (사용자 ID)</param>
        /// <returns>WebSocket 연결 객체 (없으면 null)</returns>
        IClientConnection? GetConnection(string sessionId);
    }
}