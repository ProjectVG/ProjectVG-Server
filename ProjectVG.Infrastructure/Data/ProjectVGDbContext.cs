using Microsoft.EntityFrameworkCore;
using ProjectVG.Domain.Entities.Character;
using ProjectVG.Domain.Entities.ConversationHistory;
using ProjectVG.Domain.Entities.User;

namespace ProjectVG.Infrastructure.Data
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

            // User 엔티티 설정
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

            // Character 엔티티 설정
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

            // ConversationHistory 엔티티 설정
            modelBuilder.Entity<ConversationHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.SessionId).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(4000);
                entity.Property(e => e.MetadataJson).HasMaxLength(4000);
                
                // 외래키 관계 설정 (Navigation Properties 없이)
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<Character>()
                    .WithMany()
                    .HasForeignKey(e => e.CharacterId)
                    .OnDelete(DeleteBehavior.SetNull);

                // 인덱스 설정
                entity.HasIndex(e => e.SessionId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CharacterId);
                entity.HasIndex(e => e.Timestamp);
            });

            // 기본 데이터 삽입
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var defaultCharacters = new List<Character>
            {
                new Character
                {
                    Id = Guid.Parse("1"),
                    Name = "Test Character",
                    Description = "test description",
                    Role = "test role",
                    Personality = "test personality",
                    Background = "test background",
                    IsActive = true,
                    Metadata = new Dictionary<string, string>
                    {
                        { "test", "test" }
                    },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            var defaultUsers = new List<User>
            {
                new User
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Username = "testuser",
                    Name = "Test User",
                    Email = "test@test.com",
                    Provider = "test",
                    ProviderId = "test",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            modelBuilder.Entity<Character>().HasData(defaultCharacters);
            modelBuilder.Entity<User>().HasData(defaultUsers);
        }
    }
} 