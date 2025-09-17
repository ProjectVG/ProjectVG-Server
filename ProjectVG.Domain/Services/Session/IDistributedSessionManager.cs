using ProjectVG.Common.Models.Session;

namespace ProjectVG.Domain.Services.Session
{
    /// <summary>
    /// Redis 기반 분산 세션 관리자 인터페이스
    /// </summary>
    public interface IDistributedSessionManager
    {
        /// <summary>
        /// 세션을 등록합니다 (특정 서버에)
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="serverId">서버 ID</param>
        /// <param name="sessionInfo">세션 정보</param>
        Task RegisterSessionAsync(string userId, string serverId, SessionInfo sessionInfo);

        /// <summary>
        /// 세션을 해제합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        Task UnregisterSessionAsync(string userId);

        /// <summary>
        /// 사용자가 연결된 서버 ID를 조회합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>서버 ID (연결되지 않은 경우 null)</returns>
        Task<string?> GetUserServerAsync(string userId);

        /// <summary>
        /// 세션 정보를 조회합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>세션 정보 (없으면 null)</returns>
        Task<SessionInfo?> GetSessionInfoAsync(string userId);

        /// <summary>
        /// 특정 서버에 연결된 모든 사용자 ID를 조회합니다
        /// </summary>
        /// <param name="serverId">서버 ID</param>
        /// <returns>사용자 ID 목록</returns>
        Task<IEnumerable<string>> GetServerUsersAsync(string serverId);

        /// <summary>
        /// 세션이 활성화되어 있는지 확인합니다
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>활성화 여부</returns>
        Task<bool> IsSessionActiveAsync(string userId);

        /// <summary>
        /// 전체 활성 세션 수를 반환합니다
        /// </summary>
        /// <returns>활성 세션 수</returns>
        Task<int> GetActiveSessionCountAsync();

        /// <summary>
        /// 서버를 등록합니다
        /// </summary>
        /// <param name="serverId">서버 ID</param>
        /// <param name="serverInfo">서버 정보</param>
        Task RegisterServerAsync(string serverId, ServerInfo serverInfo);

        /// <summary>
        /// 서버를 해제합니다 (연결된 모든 세션도 함께 정리)
        /// </summary>
        /// <param name="serverId">서버 ID</param>
        Task UnregisterServerAsync(string serverId);

        /// <summary>
        /// 활성 서버 목록을 조회합니다
        /// </summary>
        /// <returns>서버 ID 목록</returns>
        Task<IEnumerable<string>> GetActiveServersAsync();

        /// <summary>
        /// 만료된 세션들을 정리합니다
        /// </summary>
        /// <param name="expirationTime">만료 시간</param>
        Task CleanupExpiredSessionsAsync(TimeSpan expirationTime);
    }
}