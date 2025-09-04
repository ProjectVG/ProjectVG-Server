using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPublicToCharacter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Characters",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "IsPublic", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 2, 4, 40, 34, 142, DateTimeKind.Utc).AddTicks(7515), true, new DateTime(2025, 9, 2, 4, 40, 34, 142, DateTimeKind.Utc).AddTicks(7515) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "IsPublic", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 2, 4, 40, 34, 142, DateTimeKind.Utc).AddTicks(7613), true, new DateTime(2025, 9, 2, 4, 40, 34, 142, DateTimeKind.Utc).AddTicks(7613) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 2, 4, 40, 34, 142, DateTimeKind.Utc).AddTicks(7628), new DateTime(2025, 9, 2, 4, 40, 34, 142, DateTimeKind.Utc).AddTicks(7629) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 2, 4, 40, 34, 142, DateTimeKind.Utc).AddTicks(7631), new DateTime(2025, 9, 2, 4, 40, 34, 142, DateTimeKind.Utc).AddTicks(7631) });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_IsPublic",
                table: "Characters",
                column: "IsPublic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Characters_IsPublic",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Characters");

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 2, 3, 21, 39, 701, DateTimeKind.Utc).AddTicks(2031), new DateTime(2025, 9, 2, 3, 21, 39, 701, DateTimeKind.Utc).AddTicks(2031) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 2, 3, 21, 39, 701, DateTimeKind.Utc).AddTicks(2093), new DateTime(2025, 9, 2, 3, 21, 39, 701, DateTimeKind.Utc).AddTicks(2094) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 2, 3, 21, 39, 701, DateTimeKind.Utc).AddTicks(2110), new DateTime(2025, 9, 2, 3, 21, 39, 701, DateTimeKind.Utc).AddTicks(2111) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 2, 3, 21, 39, 701, DateTimeKind.Utc).AddTicks(2113), new DateTime(2025, 9, 2, 3, 21, 39, 701, DateTimeKind.Utc).AddTicks(2113) });
        }
    }
}
