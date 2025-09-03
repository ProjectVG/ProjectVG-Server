using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenSystemToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InitialTokensGranted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TokenBalance",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTokensEarned",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTokensSpent",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 3, 20, 58, 519, DateTimeKind.Utc).AddTicks(5684), new DateTime(2025, 9, 3, 3, 20, 58, 519, DateTimeKind.Utc).AddTicks(5685) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 3, 20, 58, 519, DateTimeKind.Utc).AddTicks(5780), new DateTime(2025, 9, 3, 3, 20, 58, 519, DateTimeKind.Utc).AddTicks(5780) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "InitialTokensGranted", "TokenBalance", "TotalTokensEarned", "TotalTokensSpent", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 3, 20, 58, 519, DateTimeKind.Utc).AddTicks(5812), false, 0m, 0m, 0m, new DateTime(2025, 9, 3, 3, 20, 58, 519, DateTimeKind.Utc).AddTicks(5812) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "InitialTokensGranted", "TokenBalance", "TotalTokensEarned", "TotalTokensSpent", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 3, 20, 58, 519, DateTimeKind.Utc).AddTicks(5814), false, 0m, 0m, 0m, new DateTime(2025, 9, 3, 3, 20, 58, 519, DateTimeKind.Utc).AddTicks(5814) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialTokensGranted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TokenBalance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TotalTokensEarned",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TotalTokensSpent",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 0, 24, 50, 269, DateTimeKind.Utc).AddTicks(1854), new DateTime(2025, 9, 3, 0, 24, 50, 269, DateTimeKind.Utc).AddTicks(1854) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 0, 24, 50, 269, DateTimeKind.Utc).AddTicks(1914), new DateTime(2025, 9, 3, 0, 24, 50, 269, DateTimeKind.Utc).AddTicks(1914) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 0, 24, 50, 269, DateTimeKind.Utc).AddTicks(1931), new DateTime(2025, 9, 3, 0, 24, 50, 269, DateTimeKind.Utc).AddTicks(1931) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 0, 24, 50, 269, DateTimeKind.Utc).AddTicks(1933), new DateTime(2025, 9, 3, 0, 24, 50, 269, DateTimeKind.Utc).AddTicks(1933) });
        }
    }
}
