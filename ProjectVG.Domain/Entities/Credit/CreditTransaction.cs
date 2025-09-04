using ProjectVG.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectVG.Domain.Entities.Credits
{
    /// <summary>
    /// 토큰 거래 내역 엔티티
    /// 모든 토큰 증감 이력을 추적하여 감사와 투명성을 보장
    /// </summary>
    [Table("CreditTransactions")]
    public class CreditTransaction : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        public Guid UserId { get; set; }
        
        /// <summary>
        /// 거래 고유 식별자 (중복 방지용)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string TransactionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 거래 유형 (EARN: 획득, SPEND: 사용)
        /// </summary>
        [Required]
        public CreditTransactionType Type { get; set; }
        
        [Required]
        [Precision(18, 2)]
        public decimal Amount { get; set; }
        
        /// <summary>
        /// 거래 후 잔액
        /// </summary>
        [Required]
        [Precision(18, 2)]
        public decimal BalanceAfter { get; set; }
        
        /// <summary>
        /// 거래 발생원 (LOGIN_BONUS, CHAT_USAGE, PAYMENT, etc.)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Source { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// 관련 엔티티 ID (채팅 세션, 결제 등)
        /// </summary>
        [StringLength(100)]
        public string? RelatedEntityId { get; set; }
        
        /// <summary>
        /// 관련 엔티티 유형 (ChatSession, Payment 등)
        /// </summary>
        [StringLength(100)]
        public string? RelatedEntityType { get; set; }

        public virtual Users.User User { get; set; } = null!;
    }

    /// <summary>
    /// 토큰 거래 유형
    /// </summary>
    public enum CreditTransactionType
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