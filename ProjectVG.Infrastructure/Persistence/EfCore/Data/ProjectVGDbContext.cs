using Microsoft.EntityFrameworkCore;
using ProjectVG.Domain.Entities.Characters;
using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Domain.Entities.Users;
using ProjectVG.Domain.Entities.Auth;
using ProjectVG.Common.Constants;

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
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users 엔티티 설정
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ProviderId).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
                
                // 인덱스 설정
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
                entity.Property(e => e.Role).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Personality).HasMaxLength(1000);
                entity.Property(e => e.Background).HasMaxLength(2000);
                
                // Metadata를 JSON으로 저장
                entity.Property(e => e.Metadata)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
                    );
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

            // RefreshTokens 엔터티 설정
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.ClientId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DeviceId).HasMaxLength(255);
                entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(128);
                entity.Property(e => e.ExpiresAt).IsRequired();
                entity.Property(e => e.RevokedAt);
                entity.Property(e => e.ReplacedByTokenId);
                entity.Property(e => e.LastUsedAt).IsRequired();
                entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6 지원
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                // 외래키 관계 설정
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                // 자기 참조 관계 (토큰 교체 추적)
                entity.HasOne(e => e.ReplacedByToken)
                    .WithMany(e => e.ReplacedTokens)
                    .HasForeignKey(e => e.ReplacedByTokenId)
                    .OnDelete(DeleteBehavior.SetNull);

                // 인덱스 설정
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.TokenHash).IsUnique();
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => new { e.UserId, e.ClientId });
                entity.HasIndex(e => new { e.UserId, e.DeviceId });
                entity.HasIndex(e => e.RevokedAt);
            });

            // 기본 데이터 삽입
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var defaultCharacters = AppConstants.DefaultCharacterPool.Select(p => new Character
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Role = p.Role,
                Personality = p.Personality,
                Background = "",
                IsActive = p.IsActive,
                Metadata = new Dictionary<string, string>(),
                VoiceId = p.VoiceId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            var defaultUsers = AppConstants.DefaultUserPool.Select(p => new User
            {
                Id = p.Id,
                Username = p.Username,
                Name = p.Name,
                Email = p.Email,
                Provider = p.Provider,
                ProviderId = p.ProviderId,
                IsActive = p.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            modelBuilder.Entity<Character>().HasData(defaultCharacters);
            modelBuilder.Entity<User>().HasData(defaultUsers);
        }
    }
} 