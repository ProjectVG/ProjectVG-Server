using ProjectVG.Domain.Entities.Session;

namespace ProjectVG.Application.Services.Session
{
    public interface IClientSessionService
    {
        /// <summary>
        /// 세션 생성
        /// </summary>
        /// <param name="userId">유저 ID</param>
        /// <param name="clientIP">클라이언트 IP</param>
        /// <param name="clientPort">클라이언트 포트</param>
        /// <returns>세션</returns>
        Task<ClientSession> CreateSessionAsync(Guid userId, string clientIP, int clientPort);

        /// <summary>
        /// 세션 조회
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <returns>세션</returns>
        Task<ClientSession?> GetSessionAsync(string sessionId);

        /// <summary>
        /// 유저 ID로 세션 조회
        /// </summary>
        /// <param name="userId">유저 ID</param>
        /// <returns>세션</returns>
        Task<ClientSession?> GetSessionByUserIdAsync(Guid userId);

        /// <summary>
        /// 웹소켓 연결 ID로 세션 조회
        /// </summary>
        /// <param name="connectionId">웹소켓 연결 ID</param>
        /// <returns>세션</returns>
        Task<ClientSession?> GetSessionByConnectionIdAsync(string connectionId);

        /// <summary>
        /// 세션 업데이트
        /// </summary>
        /// <param name="session">세션</param>
        /// <returns>세션</returns>
        Task<ClientSession> UpdateSessionAsync(ClientSession session);

        /// <summary>
        /// 세션 삭제
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        Task DeleteSessionAsync(string sessionId);

        /// <summary>
        /// 유저 ID로 세션 삭제
        /// </summary>
        Task DeleteSessionsByUserIdAsync(Guid userId);

        /// <summary>
        /// 세션 유효성 검사
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <returns>유효성 여부</returns>
        Task<bool> ValidateSessionAsync(string sessionId);
    }
} 