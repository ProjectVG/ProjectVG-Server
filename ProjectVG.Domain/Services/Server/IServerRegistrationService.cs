using ProjectVG.Domain.Models.Server;

namespace ProjectVG.Domain.Services.Server
{
    public interface IServerRegistrationService
    {
        /// <summary>
        /// 서버를 등록합니다
        /// </summary>
        Task RegisterServerAsync();

        /// <summary>
        /// 서버 등록을 해제합니다
        /// </summary>
        Task UnregisterServerAsync();

        /// <summary>
        /// 헬스체크를 수행합니다
        /// </summary>
        Task SendHeartbeatAsync();

        /// <summary>
        /// 현재 서버 ID를 가져옵니다
        /// </summary>
        string GetServerId();

        /// <summary>
        /// 활성 서버 목록을 가져옵니다
        /// </summary>
        Task<IEnumerable<ServerInfo>> GetActiveServersAsync();

        /// <summary>
        /// 특정 사용자가 연결된 서버 ID를 가져옵니다
        /// </summary>
        Task<string?> GetUserServerAsync(string userId);

        /// <summary>
        /// 사용자와 서버 매핑을 설정합니다
        /// </summary>
        Task SetUserServerAsync(string userId, string serverId);

        /// <summary>
        /// 사용자와 서버 매핑을 제거합니다
        /// </summary>
        Task RemoveUserServerAsync(string userId);

        /// <summary>
        /// 오프라인 서버들을 정리합니다
        /// </summary>
        Task CleanupOfflineServersAsync();
    }
}