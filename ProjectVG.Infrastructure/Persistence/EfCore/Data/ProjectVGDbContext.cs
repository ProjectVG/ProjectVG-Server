using Microsoft.EntityFrameworkCore;
using ProjectVG.Domain.Entities.Characters;
using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Domain.Entities.Users;
using ProjectVG.Infrastructure.Persistence.Data;

namespace ProjectVG.Infrastructure.Persistence.EfCore
{
    public class ProjectVGDbContext : DbContext
    {
        public ProjectVGDbContext(DbContextOptions<ProjectVGDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<ConversationHistory> ConversationHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users 엔티티 설정
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.UID).IsRequired().HasMaxLength(16);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ProviderId).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired();
                
                // 인덱스 설정
                entity.HasIndex(e => e.UID).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.ProviderId);
            });

            // Characters 엔티티 설정
            modelBuilder.Entity<Character>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ImageUrl).HasMaxLength(500);
                entity.Property(e => e.VoiceId).HasMaxLength(100);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
                
                // User 관계 설정 (nullable)
                entity.Property(e => e.UserId).IsRequired(false);
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Characters)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // 공개 여부 설정
                entity.Property(e => e.IsPublic).IsRequired().HasDefaultValue(true);
                
                // 설정 모드
                entity.Property(e => e.ConfigMode).IsRequired().HasDefaultValue(CharacterConfigMode.Individual);
                
                // JSON 설정 (개별 설정용)
                entity.Property(e => e.IndividualConfigJson).HasColumnType("nvarchar(max)");
                
                // SystemPrompt (직접 입력용, 최대 5000자)
                entity.Property(e => e.SystemPrompt).HasMaxLength(5000);
                
                // JSON 컬럼 변환 설정
                entity.Property(e => e.IndividualConfig)
                    .HasConversion(
                        v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<IndividualConfig>(v, (System.Text.Json.JsonSerializerOptions?)null))
                    .HasColumnName("IndividualConfigJson");
                
                // 제약 조건
                entity.ToTable(t => t.HasCheckConstraint("CK_Character_ConfigMode_Valid", "ConfigMode IN (0, 1)"));
                
                // 인덱스
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.ConfigMode);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.IsPublic);
            });

            // ConversationHistorys 엔티티 설정
            modelBuilder.Entity<ConversationHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Content).IsRequired().HasMaxLength(4000);
                entity.Property(e => e.MetadataJson).HasMaxLength(4000);

                // 외래키 관계 설정 (UserId, CharacterId는 필수)
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                entity.HasOne<Character>()
                    .WithMany()
                    .HasForeignKey(e => e.CharacterId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                // 복합 인덱스: UserId + CharacterId + Timestamp
                entity.HasIndex(e => new { e.UserId, e.CharacterId, e.Timestamp });
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CharacterId);
                entity.HasIndex(e => e.Timestamp);
            });

            // 기본 데이터 삽입
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var defaultCharacters = DatabaseSeedData.DefaultCharacterPool.Select(p => new Character
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                IsActive = p.IsActive,
                VoiceId = p.VoiceId,
                ConfigMode = CharacterConfigMode.Individual,
                IndividualConfigJson = System.Text.Json.JsonSerializer.Serialize(new IndividualConfig
                {
                    Role = p.Role,
                    Personality = p.Personality,
                    SpeechStyle = p.SpeechStyle,
                    UserAlias = p.UserAlias,
                    Summary = p.Summary,
                    Background = ""
                }),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            var defaultUsers = DatabaseSeedData.DefaultUserPool.Select(p => new User
            {
                Id = p.Id,
                UID = p.UID,
                Username = p.Username,
                Email = p.Email,
                Provider = p.Provider,
                ProviderId = p.ProviderId,
                Status = p.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            modelBuilder.Entity<Character>().HasData(defaultCharacters);
            modelBuilder.Entity<User>().HasData(defaultUsers);
        }
    }
} 