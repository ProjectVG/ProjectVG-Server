using ProjectVG.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;


namespace ProjectVG.Domain.Entities.ConversationHistorys
{
    /// <summary>
    /// 사용자와 AI 캐릭터 간의 대화 기록
    /// </summary>
    [Table("ConversationHistories")]
    public class ConversationHistory : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public Guid CharacterId { get; set; }
        
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [StringLength(20)]
        [RegularExpression("^(User|Assistant|System)$")]
        public string Role { get; set; } = string.Empty;
        
        [Required]
        [StringLength(10000)]
        public string Content { get; set; } = string.Empty;
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        [StringLength(100)]
        public string? ConversationId { get; set; }
    }
} 