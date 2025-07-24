using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProjectVG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Personality = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SpeechStyle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Background = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VoiceId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConversationHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationHistories_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConversationHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Characters",
                columns: new[] { "Id", "Background", "CreatedAt", "Description", "IsActive", "Metadata", "Name", "Personality", "Role", "SpeechStyle", "UpdatedAt", "VoiceId" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "", new DateTime(2025, 7, 24, 14, 54, 13, 93, DateTimeKind.Utc).AddTicks(3060), "20대 대학생 여사친 느낌의 귀엽고 발랄한 AI 캐릭터", true, "{}", "하루", "장난기 많고 친근하며 감정 표현이 풍부함", "여사친 또는 여친 같은 존재", "", new DateTime(2025, 7, 24, 14, 54, 13, 93, DateTimeKind.Utc).AddTicks(3060), "haru" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "", new DateTime(2025, 7, 24, 14, 54, 13, 93, DateTimeKind.Utc).AddTicks(3063), "게임 속 조력자 같은 꼬마 마법사 느낌의 AI 캐릭터", true, "{}", "미야", "조금 새침하고 똑똑하며, 호기심이 많고 귀여움", "어린 조력자", "", new DateTime(2025, 7, 24, 14, 54, 13, 93, DateTimeKind.Utc).AddTicks(3063), "miya" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "", new DateTime(2025, 7, 24, 14, 54, 13, 93, DateTimeKind.Utc).AddTicks(3065), "조용하고 나긋나긋한 말투의 메이드 느낌 AI 캐릭터", true, "{}", "소피아", "차분하고 배려심 깊으며 살짝 순종적", "헌신적인 메이드", "", new DateTime(2025, 7, 24, 14, 54, 13, 93, DateTimeKind.Utc).AddTicks(3065), "sophia" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "Name", "Provider", "ProviderId", "UpdatedAt", "Username" },
                values: new object[] { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2025, 7, 24, 14, 54, 13, 93, DateTimeKind.Utc).AddTicks(3082), "test@test.com", true, "Test User", "test", "test", new DateTime(2025, 7, 24, 14, 54, 13, 93, DateTimeKind.Utc).AddTicks(3082), "testuser" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationHistories_CharacterId",
                table: "ConversationHistories",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationHistories_Timestamp",
                table: "ConversationHistories",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationHistories_UserId",
                table: "ConversationHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationHistories_UserId_CharacterId_Timestamp",
                table: "ConversationHistories",
                columns: new[] { "UserId", "CharacterId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProviderId",
                table: "Users",
                column: "ProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationHistories");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
