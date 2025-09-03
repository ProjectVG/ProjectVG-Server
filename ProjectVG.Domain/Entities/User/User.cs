using ProjectVG.Domain.Common;


namespace ProjectVG.Domain.Entities.Users
{
    /// <summary>
    /// 사용자 엔티티
    /// 
    /// ID 관리:
    /// - Id: 내부 DB용 GUID
    /// - UID: 외부 노출용 12자리 고유 ID (인덱스 설정)
    /// 
    /// OAuth2 지원:
    /// - ProviderId, Provider: OAuth2 로그인 시에만 사용
    /// </summary>
    public class User : BaseEntity
    {
        /// <summary>
        /// 내부용 고유 ID (GUID)
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// 외부 노출용 고유 ID (12자리)
        /// </summary>
        public string UID { get; set; } = string.Empty;
        
        /// <summary>
        /// OAuth2 Provider ID
        /// </summary>
        public string ProviderId { get; set; } = string.Empty;
        
        /// <summary>
        /// OAuth2 Provider (google, github 등)
        /// </summary>
        public string Provider { get; set; } = string.Empty;
        
        /// <summary>
        /// 사용자 이메일
        /// </summary>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// 사용자 표시명
        /// </summary>
        public string Username { get; set; } = string.Empty;
        
        /// <summary>
        /// 계정 상태
        /// </summary>
        public AccountStatus Status { get; set; } = AccountStatus.Active;
        
        /// <summary>
        /// 현재 크래딧 잔액 (1 Cost = 1 Credit)
        /// </summary>
        public decimal CreditBalance { get; set; } = 0;
        
        /// <summary>
        /// 총 획득한 크래딧 수 (누적)
        /// </summary>
        public decimal TotalCreditsEarned { get; set; } = 0;
        
        /// <summary>
        /// 총 사용한 크래딧 수 (누적)
        /// </summary>
        public decimal TotalCreditsSpent { get; set; } = 0;
        
        /// <summary>
        /// 첫 로그인 크래딧 지급 여부
        /// </summary>
        public bool InitialCreditsGranted { get; set; } = false;

        /// <summary>
        /// 사용자가 생성한 캐릭터들 (네비게이션 속성)
        /// </summary>
        public virtual ICollection<Characters.Character> Characters { get; set; } = new List<Characters.Character>();
    }
} 