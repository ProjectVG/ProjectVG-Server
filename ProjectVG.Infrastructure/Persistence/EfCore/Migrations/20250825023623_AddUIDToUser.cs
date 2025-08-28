using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Persistence.EfCore
{
    /// <inheritdoc />
    public partial class AddUIDToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UID",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5763), new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5763) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5766), new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5766) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5768), new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5768) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5770), new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5770) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "Name", "UID", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5783), "Test User", "", new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5784) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "Name", "UID", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5786), "Zero User", "", new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5786) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UID",
                table: "Users");

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
