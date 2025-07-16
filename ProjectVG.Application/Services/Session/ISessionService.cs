using ProjectVG.Infrastructure.Services.Session;
using System.Net.WebSockets;

namespace ProjectVG.Application.Services.Session
{
    public interface ISessionService
    {
        /// <summary>
        /// 세션을 등록합니다
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="userId">사용자 ID (선택사항)</param>
        Task RegisterSessionAsync(string sessionId, WebSocket socket, string? userId = null);

        /// <summary>
        /// 세션을 해제합니다
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        Task UnregisterSessionAsync(string sessionId);

        /// <summary>
        /// 세션에 메시지를 전송합니다
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="message">전송할 메시지</param>
        Task SendMessageAsync(string sessionId, string message);

        /// <summary>
        /// 세션에 오디오를 전송합니다
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="audioData">전송할 오디오 데이터</param>
        /// <param name="contentType">오디오 타입</param>
        /// <param name="audioLength">오디오 길이</param>
        /// <returns></returns>
        Task SendAudioAsync(string sessionId, byte[] audioData, string? contentType = null, float? audioLength = null);

        /// <summary>
        /// 세션 정보를 조회합니다
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <returns>세션 정보</returns>
        Task<ClientConnection?> GetSessionAsync(string sessionId);

        /// <summary>
        /// 모든 세션을 조회합니다
        /// </summary>
        /// <returns>모든 세션 목록</returns>
        Task<IEnumerable<ClientConnection>> GetAllSessionsAsync();

        /// <summary>
        /// 활성 세션 수를 조회합니다
        /// </summary>
        /// <returns>활성 세션 수</returns>
        Task<int> GetActiveSessionCountAsync();
    }
} 