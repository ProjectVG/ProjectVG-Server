using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateConversationHistorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ConversationHistories");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "ConversationHistories");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "ConversationHistories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "ConversationHistories",
                type: "nvarchar(max)",
                maxLength: 10000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddColumn<string>(
                name: "ConversationId",
                table: "ConversationHistories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_ConversationHistories_ConversationId",
                table: "ConversationHistories",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationHistories_Role",
                table: "ConversationHistories",
                column: "Role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConversationHistories_ConversationId",
                table: "ConversationHistories");

            migrationBuilder.DropIndex(
                name: "IX_ConversationHistories_Role",
                table: "ConversationHistories");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "ConversationHistories");

            migrationBuilder.AlterColumn<int>(
                name: "Role",
                table: "ConversationHistories",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "ConversationHistories",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 10000);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ConversationHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "ConversationHistories",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 2, 4, 40, 34, 142, DateTimeKind.Utc).AddTicks(7515), new DateTime(2025, 9, 2, 4, 40, 34, 142, DateTimeKind.Utc).AddTicks(7515) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 2, 4, 40, 34, 142, DateTimeKind.Utc).AddTicks(7613), new DateTime(2025, 9, 2, 4, 40, 34, 142, DateTimeKind.Utc).AddTicks(7613) });

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
        }
    }
}
