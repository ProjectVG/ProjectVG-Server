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

        /// <summary>
        /// EF Core 모델을 구성합니다.
        /// </summary>
        /// <remarks>
        /// 엔티티별 스키마 및 제약을 구성합니다:
        /// - User: 기본 키(Id), UID(필수, 최대 16), Email(필수, 최대 255), Username(필수, 최대 50), ProviderId(필수, 최대 255), Provider(필수, 최대 50), Status(필수) 및 UID/Email에 대한 고유 인덱스와 ProviderId 인덱스.
        /// - Character: 기본 키(Id), Name(필수, 최대 100), Description(최대 1000), Role(필수, 최대 500), Personality(최대 1000), Background(최대 2000).
        /// - ConversationHistory: 기본 키(Id), Content(필수, 최대 4000), MetadataJson(최대 4000), User 및 Character에 대한 필수 외래키(삭제 시 Cascade), 그리고 UserId+CharacterId+Timestamp 복합 인덱스 및 개별 인덱스들.
        /// 메서드 마지막에 SeedData(modelBuilder)를 호출하여 초기 데이터를 주입합니다.
        /// </remarks>
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
                entity.Property(e => e.Role).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Personality).HasMaxLength(1000);
                entity.Property(e => e.Background).HasMaxLength(2000);
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

        /// <summary>
        /// 데이터베이스 모델에 초기 시드 데이터를 추가합니다.
        /// </summary>
        /// <param name="modelBuilder">엔터티 구성을 위한 ModelBuilder. DatabaseSeedData의 기본 캐릭터 풀과 사용자 풀을 사용하여 Character와 User 엔터티에 초기 데이터를 등록합니다.</param>
        /// <remarks>
        /// - Character 엔터티: DatabaseSeedData.DefaultCharacterPool에서 복사하여 Background는 빈 문자열로 설정하고 CreatedAt/UpdatedAt는 UTC 현재 시각으로 설정합니다.
        /// - User 엔터티: DatabaseSeedData.DefaultUserPool에서 복사하여 UID, Status 등을 포함하고 CreatedAt/UpdatedAt는 UTC 현재 시각으로 설정합니다.
        /// - ConversationHistory에 대한 시드 데이터는 추가하지 않습니다.
        /// </remarks>
        private void SeedData(ModelBuilder modelBuilder)
        {
            var defaultCharacters = DatabaseSeedData.DefaultCharacterPool.Select(p => new Character
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Role = p.Role,
                Personality = p.Personality,
                Background = "",
                IsActive = p.IsActive,
                VoiceId = p.VoiceId,
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