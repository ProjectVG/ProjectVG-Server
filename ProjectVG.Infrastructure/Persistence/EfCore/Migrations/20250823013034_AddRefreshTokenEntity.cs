using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Persistence.EfCore
{
    /// <inheritdoc />
    public partial class AddRefreshTokenEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId",
                        column: x => x.ReplacedByTokenId,
                        principalTable: "RefreshTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 23, 1, 30, 34, 660, DateTimeKind.Utc).AddTicks(1665), new DateTime(2025, 8, 23, 1, 30, 34, 660, DateTimeKind.Utc).AddTicks(1666) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 23, 1, 30, 34, 660, DateTimeKind.Utc).AddTicks(1668), new DateTime(2025, 8, 23, 1, 30, 34, 660, DateTimeKind.Utc).AddTicks(1668) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 23, 1, 30, 34, 660, DateTimeKind.Utc).AddTicks(1670), new DateTime(2025, 8, 23, 1, 30, 34, 660, DateTimeKind.Utc).AddTicks(1670) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 23, 1, 30, 34, 660, DateTimeKind.Utc).AddTicks(1672), new DateTime(2025, 8, 23, 1, 30, 34, 660, DateTimeKind.Utc).AddTicks(1672) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 23, 1, 30, 34, 660, DateTimeKind.Utc).AddTicks(1687), "Test User", new DateTime(2025, 8, 23, 1, 30, 34, 660, DateTimeKind.Utc).AddTicks(1687) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 23, 1, 30, 34, 660, DateTimeKind.Utc).AddTicks(1689), "Zero User", new DateTime(2025, 8, 23, 1, 30, 34, 660, DateTimeKind.Utc).AddTicks(1689) });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplacedByTokenId",
                table: "RefreshTokens",
                column: "ReplacedByTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_RevokedAt",
                table: "RefreshTokens",
                column: "RevokedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ClientId",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ClientId" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_DeviceId",
                table: "RefreshTokens",
                columns: new[] { "UserId", "DeviceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2689), new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2689) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2692), new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2692) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2694), new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2694) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2696), new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2696) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2710), "Test Users", new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2710) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "Name", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2712), "Zero Users", new DateTime(2025, 7, 24, 23, 1, 32, 761, DateTimeKind.Utc).AddTicks(2712) });
        }
    }
}
