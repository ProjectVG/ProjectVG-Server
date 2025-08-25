using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Persistence.EfCore
{
    /// <inheritdoc />
    public partial class UpdateUserEntityWithUIDAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "UID",
                table: "Users",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4397), new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4398) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4402), new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4403) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4406), new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4406) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4408), new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4408) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "Status", "UID", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4427), 1, "TESTUSER001", new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4427) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "Status", "UID", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4430), 1, "ZEROUSER001", new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4430) });

            migrationBuilder.CreateIndex(
                name: "IX_Users_UID",
                table: "Users",
                column: "UID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_UID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "UID",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(12)",
                oldMaxLength: 12);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8922), new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8922) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8925), new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8925) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8927), new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8927) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8929), new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8929) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "IsActive", "Name", "UID", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8942), true, "Test User", "", new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8943) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "IsActive", "Name", "UID", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8944), true, "Zero User", "", new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8945) });
        }
    }
}
