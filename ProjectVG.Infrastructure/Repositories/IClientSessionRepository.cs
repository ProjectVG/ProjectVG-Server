using ProjectVG.Domain.Entities.Session;

namespace ProjectVG.Infrastructure.Repositories
{
    public interface IClientSessionRepository
    {
        /// <summary>
        /// 세션 ID로 세션 조회
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        /// <returns>세션</returns>
        Task<ClientSession?> GetBySessionIdAsync(string sessionId);
        Task<ClientSession?> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// 웹소켓 연결 ID로 세션 조회
        /// </summary>
        /// <param name="connectionId">웹소켓 연결 ID</param>
        /// <returns>세션</returns>
        Task<ClientSession?> GetByConnectionIdAsync(string connectionId);

        /// <summary>
        /// 세션 생성
        /// </summary>
        /// <param name="session">세션</param>
        /// <returns>세션</returns>
        Task<ClientSession> CreateAsync(ClientSession session);

        /// <summary>
        /// 세션 업데이트
        /// </summary>
        /// <param name="session">세션</param>
        /// <returns>세션</returns>
        Task<ClientSession> UpdateAsync(ClientSession session);

        /// <summary>
        /// 세션 삭제
        /// </summary>
        /// <param name="sessionId">세션 ID</param>
        Task DeleteAsync(string sessionId);

        /// <summary>
        /// 유저 ID로 세션 삭제
        /// </summary>
        /// <param name="userId">유저 ID</param>
        Task DeleteByUserIdAsync(Guid userId);
    }
} 