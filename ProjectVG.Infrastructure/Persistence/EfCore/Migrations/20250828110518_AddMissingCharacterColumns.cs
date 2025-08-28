using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Persistence.EfCore
{
    /// <inheritdoc />
    public partial class AddMissingCharacterColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "Characters",
                newName: "UserAlias");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Characters",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Characters",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "ImageUrl", "Summary", "UpdatedAt", "UserAlias" },
                values: new object[] { new DateTime(2025, 8, 28, 11, 5, 18, 233, DateTimeKind.Utc).AddTicks(3705), "", "", new DateTime(2025, 8, 28, 11, 5, 18, 233, DateTimeKind.Utc).AddTicks(3705), "" });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "ImageUrl", "Summary", "UpdatedAt", "UserAlias" },
                values: new object[] { new DateTime(2025, 8, 28, 11, 5, 18, 233, DateTimeKind.Utc).AddTicks(3707), "", "", new DateTime(2025, 8, 28, 11, 5, 18, 233, DateTimeKind.Utc).AddTicks(3708), "" });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "ImageUrl", "Summary", "UpdatedAt", "UserAlias" },
                values: new object[] { new DateTime(2025, 8, 28, 11, 5, 18, 233, DateTimeKind.Utc).AddTicks(3709), "", "", new DateTime(2025, 8, 28, 11, 5, 18, 233, DateTimeKind.Utc).AddTicks(3710), "" });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "ImageUrl", "Summary", "UpdatedAt", "UserAlias" },
                values: new object[] { new DateTime(2025, 8, 28, 11, 5, 18, 233, DateTimeKind.Utc).AddTicks(3711), "", "", new DateTime(2025, 8, 28, 11, 5, 18, 233, DateTimeKind.Utc).AddTicks(3711), "" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 28, 11, 5, 18, 233, DateTimeKind.Utc).AddTicks(3721), new DateTime(2025, 8, 28, 11, 5, 18, 233, DateTimeKind.Utc).AddTicks(3722) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 28, 11, 5, 18, 233, DateTimeKind.Utc).AddTicks(3724), new DateTime(2025, 8, 28, 11, 5, 18, 233, DateTimeKind.Utc).AddTicks(3724) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Characters");

            migrationBuilder.RenameColumn(
                name: "UserAlias",
                table: "Characters",
                newName: "Metadata");

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "Metadata", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1753), "{}", new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1753) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "Metadata", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1756), "{}", new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1756) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "Metadata", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1758), "{}", new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1758) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "Metadata", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1789), "{}", new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1789) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1801), new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1801) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1803), new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1803) });
        }
    }
}
