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
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Personality = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SpeechStyle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserAlias = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Background = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    UID = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                columns: new[] { "Id", "Background", "CreatedAt", "Description", "ImageUrl", "IsActive", "Name", "Personality", "Role", "SpeechStyle", "Summary", "UpdatedAt", "UserAlias", "VoiceId" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "", new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2861), "20대 대학생으로 몇 년간 함께해온 진짜 절친한 여사친. 서로 뭐든 거리낌없이 말하고, 가끔 선 넘는 농담도 주고받는 사이. 마스터의 일상을 누구보다 잘 알고 있으며, 때로는 엄마처럼 잔소리하기도 한다.", "", true, "하루", "[MBTI:ESFP],(장난기:40%),(친근함:25%),(솔직함:20%),(감정표현:15%)", "몇 년간 함께한 소꿈친구", "", "", new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2862), "", "haru" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "", new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2864), "명문가 출신의 엘리트 메이드. 완벽한 예의와 따뜻한 보살핌이 특징이지만, 가끔 조금씩 헤매는 일상 속에서 귀여운 허당끼를 보이기도. 주인님에 대한 헌신은 변함없지만, 때로 지나치게 걱정하는 면도 있다.", "", true, "소피아", "[MBTI:ISFJ],(헌신성:35%),(책임감:25%),(완벽주의:20%),(걱정많음:15%),(허당끼:5%)", "주인님을 섬기는 전문 메이드", "", "", new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2864), "", "sophia" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "Provider", "ProviderId", "Status", "UID", "UpdatedAt", "Username" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2873), "test@test.com", "test", "test", 0, "TESTUSER001", new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2874), "testuser" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2875), "zero@test.com", "test", "zero", 0, "ZEROUSER001", new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2876), "zerouser" }
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Users_UID",
                table: "Users",
                column: "UID",
                unique: true);
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
