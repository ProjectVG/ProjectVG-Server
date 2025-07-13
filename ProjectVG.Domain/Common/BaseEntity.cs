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
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Update()
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
} 