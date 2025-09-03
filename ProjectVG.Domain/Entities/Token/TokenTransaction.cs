using ProjectVG.Domain.Common;

namespace ProjectVG.Domain.Entities.Tokens
{
    /// <summary>
    /// 토큰 거래 내역 엔티티
    /// 모든 토큰 증감 이력을 추적하여 감사와 투명성을 보장
    /// </summary>
    public class TokenTransaction : BaseEntity
    {
        /// <summary>
        /// 내부용 고유 ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 사용자 ID (외래키)
        /// </summary>
        public Guid UserId { get; set; }
        
        /// <summary>
        /// 거래 고유 식별자 (중복 방지용)
        /// </summary>
        public string TransactionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 거래 유형 (EARN: 획득, SPEND: 사용)
        /// </summary>
        public TokenTransactionType Type { get; set; }
        
        /// <summary>
        /// 거래 금액 (양수: 증가, 음수: 감소)
        /// </summary>
        public decimal Amount { get; set; }
        
        /// <summary>
        /// 거래 후 잔액
        /// </summary>
        public decimal BalanceAfter { get; set; }
        
        /// <summary>
        /// 거래 발생원 (LOGIN_BONUS, CHAT_USAGE, PAYMENT, etc.)
        /// </summary>
        public string Source { get; set; } = string.Empty;
        
        /// <summary>
        /// 거래 상세 설명
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 관련 엔티티 ID (예: 채팅 세션 ID, 결제 ID)
        /// </summary>
        public string? RelatedEntityId { get; set; }
        
        /// <summary>
        /// 관련 엔티티 유형 (예: ChatSession, Payment)
        /// </summary>
        public string? RelatedEntityType { get; set; }

        /// <summary>
        /// 사용자 엔티티 (네비게이션 속성)
        /// </summary>
        public virtual Users.User User { get; set; } = null!;
    }

    /// <summary>
    /// 토큰 거래 유형
    /// </summary>
    public enum TokenTransactionType
    {
        /// <summary>
        /// 토큰 획득 (로그인 보너스, 결제, 이벤트 등)
        /// </summary>
        Earn = 1,
        
        /// <summary>
        /// 토큰 사용 (채팅, 서비스 이용 등)
        /// </summary>
        Spend = 2
    }
}