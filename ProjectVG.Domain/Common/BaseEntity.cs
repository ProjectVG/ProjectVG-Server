namespace ProjectVG.Domain.Common
{
    public abstract class BaseEntity
    {
        // 생성 시각
        public DateTime CreatedAt { get; set; }
        // 수정 시각
        public DateTime? UpdatedAt { get; set; }

        protected BaseEntity()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Update()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
} 