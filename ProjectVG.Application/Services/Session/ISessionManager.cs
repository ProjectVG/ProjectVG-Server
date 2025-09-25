namespace ProjectVG.Application.Services.Session
{
    /// <summary>
    /// 세션 관리 인터페이스 - 세션 상태 관리만 담당
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// 새 세션을 생성합니다
        /// </summary>
        /// <param name="userId">사용자 ID (세션 ID로 사용됨)</param>
        /// <returns>생성된 세션 ID</returns>
        Task<string> CreateSessionAsync(Guid userId);

        /// <summary>
        /// 세션이 활성 상태인지 확인합니다
        /// </summary>
        /// <param name="userId">사용자 ID (세션 ID)</param>
        /// <returns>세션 활성 상태</returns>
        Task<bool> IsSessionActiveAsync(Guid userId);

        /// <summary>
        /// 세션의 하트비트를 업데이트합니다 (TTL 갱신)
        /// </summary>
        /// <param name="userId">사용자 ID (세션 ID)</param>
        /// <returns>업데이트 성공 여부</returns>
        Task<bool> UpdateSessionHeartbeatAsync(Guid userId);

        /// <summary>
        /// 세션을 삭제합니다
        /// </summary>
        /// <param name="userId">사용자 ID (세션 ID)</param>
        /// <returns>삭제 성공 여부</returns>
        Task<bool> DeleteSessionAsync(Guid userId);

        /// <summary>
        /// 활성 세션 수를 조회합니다
        /// </summary>
        /// <returns>활성 세션 수</returns>
        Task<int> GetActiveSessionCountAsync();

        /// <summary>
        /// 모든 활성 세션의 사용자 ID 목록을 조회합니다
        /// </summary>
        /// <returns>활성 사용자 ID 목록</returns>
        Task<IEnumerable<string>> GetActiveUserIdsAsync();
    }
}