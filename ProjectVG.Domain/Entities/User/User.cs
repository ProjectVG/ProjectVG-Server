using ProjectVG.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;


namespace ProjectVG.Domain.Entities.Users
{
    /// <summary>
    /// 사용자 엔티티 (OAuth2 및 크래딧 시스템 지원)
    /// </summary>
    [Table("Users")]
    public class User : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        
        /// <summary>
        /// 외부 노출용 12자리 고유 ID
        /// </summary>
        [Required]
        [StringLength(16)]
        public string UID { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string ProviderId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Provider { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public AccountStatus Status { get; set; } = AccountStatus.Active;
        
        [Precision(18, 2)]
        public decimal CreditBalance { get; set; } = 0;
        
        [Precision(18, 2)]
        public decimal TotalCreditsEarned { get; set; } = 0;
        
        [Precision(18, 2)]
        public decimal TotalCreditsSpent { get; set; } = 0;
        
        public bool InitialCreditsGranted { get; set; } = false;

        [Timestamp]
        public byte[] RowVersion { get; set; } = new byte[0];

        public virtual ICollection<Characters.Character> Characters { get; set; } = new List<Characters.Character>();
    }
} 