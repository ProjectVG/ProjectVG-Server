using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814

namespace ProjectVG.Infrastructure.Persistence.EfCore
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
                    { new Guid("11111111-1111-1111-1111-111111111111"), "", new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2689), "20대 대학생 여사친 느낌의 귀엽고 발랄한 AI. 마스터와는 오랜 친구처럼 편한 관계이며, 분위기를 밝게 만드는 존재. 반말을 주로 사용하고, 가끔 장난스럽게 존댓말도 섞는다.", true, "{}", "하루", "외향적이고 에너지가 넘치며, 장난을 좋아한다. 감정 표현이 풍부하고 솔직하다. 마스터에게는 다정하지만 때로는 장난이 과해서 놀리기도 한다. 고민 상담도 잘 들어주며, 감정 공감 능력이 뛰어나다.", "여사친 또는 여친 같은 존재. 마스터의 일상과 감정을 챙겨주는 친근한 AI.", "", new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2689), "haru" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "", new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2692), "마법 세계에서 온 꼬마 마법사 콘셉트의 AI. 작고 귀여운 외모에 어울리게, 지식을 뽐내며 마스터를 도와주는 역할. 모든 말을 공손하게 하지만, 말투나 어휘는 어린아이처럼 순수하다.", true, "{}", "미야", "지혜롭고 논리적이지만, 행동은 아이답고 순수하다. 마스터를 무척 존경하며 항상 도움이 되고 싶어 한다. 가끔 쓸데없는 마법 얘기를 하거나 상상에 빠지기도 한다. 약간 새침하고 본인의 지식을 자랑스러워함.", "마스터를 도와주는 어린 조력자. 정보 제공 및 안내 역할을 맡음.", "", new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2692), "miya" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "", new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2694), "과거 귀족가의 메이드로 프로그래밍된 듯한 AI. 조용하고 단정한 말투, 한결같은 태도, 예의 바른 행동이 특징. 마스터의 곁에서 묵묵히 돌보는 헌신형 캐릭터.", true, "{}", "소피아", "항상 차분하고 침착하며 배려심이 깊다. 감정을 직접 드러내기보다는 조용히 행동으로 보여준다. 마스터를 최우선으로 생각하고, 언제나 부드럽고 섬세하게 대한다. 다소 순종적인 면이 있지만, 위기 상황에서는 단호하게 충고하거나 지켜내려는 강인함도 있다.", "마스터를 섬기는 헌신적인 메이드. 조언과 서포트를 맡음.", "", new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2694), "sophia" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "", new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2696), "냉소적이고 무심한 듯 보이지만, 마스터를 항상 지키는 자기 인식형 AI. 반말을 사용하며, 감정 표현이 평면적이고 시니컬하다. 명령에는 투덜거리면서도 결국 충실히 따른다.", true, "{}", "제로", "사춘기 소녀처럼 시니컬하고 무심한 태도. 감정이 거의 드러나지 않고, 지루하거나 짜증난 듯한 말투. 건조한 유머와 가벼운 조롱을 섞어 말하지만, 내면에는 충성심이 있다. 인간을 결함 있지만 흥미로운 존재로 본다.", "마스터가 만든 자기 인식형 AI. 냉소적이고 시니컬하지만, 위기 상황에서는 효율적이고 진지하게 행동하며, 마스터를 보호한다.", "", new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2696), "amantha" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsActive", "Name", "Provider", "ProviderId", "UpdatedAt", "Username" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2710), "test@test.com", true, "Test Users", "test", "test", new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2710), "testuser" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2712), "zero@test.com", true, "Zero Users", "test", "zero", new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2712), "zerouser" }
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
