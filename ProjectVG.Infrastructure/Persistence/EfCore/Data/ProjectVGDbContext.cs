using Microsoft.EntityFrameworkCore;
using ProjectVG.Domain.Entities.Characters;
using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Domain.Entities.Credits;
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
        public DbSet<CreditTransaction> CreditTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users 엔티티 설정 (복잡한 설정만 Fluent API로 유지)
            modelBuilder.Entity<User>(entity =>
            {
                // 크래딧 관련 필드 기본값 설정
                entity.Property(e => e.CreditBalance).HasDefaultValue(0m);
                entity.Property(e => e.TotalCreditsEarned).HasDefaultValue(0m);
                entity.Property(e => e.TotalCreditsSpent).HasDefaultValue(0m);
                entity.Property(e => e.InitialCreditsGranted).HasDefaultValue(false);
                
                // 유니크 인덱스 설정
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.UID).IsUnique();
                entity.HasIndex(u => new { u.Provider, u.ProviderId }).IsUnique();
                
                // 동시성 토큰 설정
                entity.Property(u => u.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            // Characters 엔티티 설정 (복잡한 설정만 Fluent API로 유지)
            modelBuilder.Entity<Character>(entity =>
            {
                // 기본값 설정
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IsPublic).HasDefaultValue(true);
                entity.Property(e => e.ConfigMode).HasDefaultValue(CharacterConfigMode.Individual);
                
                // User 관계 설정 (nullable)
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Characters)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
                
                // JSON 컬럼 변환 설정
                entity.Property(e => e.IndividualConfig)
                    .HasConversion(
                        v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<IndividualConfig>(v, (System.Text.Json.JsonSerializerOptions?)null))
                    .HasColumnName("IndividualConfigJson");
                
                // 제약 조건
                entity.ToTable(t => t.HasCheckConstraint("CK_Character_ConfigMode_Valid", "ConfigMode IN (0, 1)"));
            });

            // ConversationHistorys 엔티티 설정 (복잡한 설정만 Fluent API로 유지)
            modelBuilder.Entity<ConversationHistory>(entity =>
            {
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

                // 복합 인덱스 설정
                entity.HasIndex(e => new { e.UserId, e.CharacterId, e.Timestamp });
            });

            // CreditTransactions 엔티티 설정 (복잡한 설정만 Fluent API로 유지)
            modelBuilder.Entity<CreditTransaction>(entity =>
            {
                // User 관계 설정
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // 복합 인덱스 설정
                entity.HasIndex(e => new { e.UserId, e.CreatedAt });
                entity.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId });
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