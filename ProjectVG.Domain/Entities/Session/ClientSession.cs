using ProjectVG.Domain.Common;

namespace ProjectVG.Domain.Entities.Session
{
    public class ClientSession : BaseEntity
    {
        /// 세션 ID
        public string SessionId { get; set; }

        /// 유저 ID
        public Guid UserId { get; set; }

        /// 클라이언트 IP
        public string ClientIP { get; set; } = string.Empty;

        /// 클라이언트 포트
        public int ClientPort { get; set; }

        /// 사용자 에이전트
        public string? UserAgent { get; set; }

        /// 세션 활성 여부
        public bool IsActive { get; set; }

        /// 세션 만료 시간
        public DateTime ExpiresAt { get; set; }

        /// 웹소켓 연결 ID
        public string? ConnectionId { get; set; }

        /// 마지막 활동 시간
        public DateTime LastActivity { get; set; }

        public void UpdateLastActivity()
        {
            LastActivity = DateTime.UtcNow;
            Update();
        }

        public void ExtendSession(TimeSpan duration)
        {
            ExpiresAt = DateTime.UtcNow.Add(duration);
            UpdateLastActivity();
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow > ExpiresAt;
        }

        public bool IsInactive(TimeSpan timeout)
        {
            return DateTime.UtcNow - LastActivity > timeout;
        }
    }
} 