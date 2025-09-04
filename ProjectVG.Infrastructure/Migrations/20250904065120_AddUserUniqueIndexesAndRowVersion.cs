using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserUniqueIndexesAndRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 4, 6, 51, 20, 117, DateTimeKind.Utc).AddTicks(7793), new DateTime(2025, 9, 4, 6, 51, 20, 117, DateTimeKind.Utc).AddTicks(7794) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 4, 6, 51, 20, 117, DateTimeKind.Utc).AddTicks(7883), new DateTime(2025, 9, 4, 6, 51, 20, 117, DateTimeKind.Utc).AddTicks(7884) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 4, 6, 51, 20, 117, DateTimeKind.Utc).AddTicks(7901), new DateTime(2025, 9, 4, 6, 51, 20, 117, DateTimeKind.Utc).AddTicks(7902) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 4, 6, 51, 20, 117, DateTimeKind.Utc).AddTicks(7904), new DateTime(2025, 9, 4, 6, 51, 20, 117, DateTimeKind.Utc).AddTicks(7905) });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Provider_ProviderId",
                table: "Users",
                columns: new[] { "Provider", "ProviderId" },
                unique: true);

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
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Provider_ProviderId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_UID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 14, 14, 43, 477, DateTimeKind.Utc).AddTicks(3555), new DateTime(2025, 9, 3, 14, 14, 43, 477, DateTimeKind.Utc).AddTicks(3555) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 14, 14, 43, 477, DateTimeKind.Utc).AddTicks(3622), new DateTime(2025, 9, 3, 14, 14, 43, 477, DateTimeKind.Utc).AddTicks(3622) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 14, 14, 43, 477, DateTimeKind.Utc).AddTicks(3649), new DateTime(2025, 9, 3, 14, 14, 43, 477, DateTimeKind.Utc).AddTicks(3650) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 14, 14, 43, 477, DateTimeKind.Utc).AddTicks(3652), new DateTime(2025, 9, 3, 14, 14, 43, 477, DateTimeKind.Utc).AddTicks(3652) });
        }
    }
}
