using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenTransactionEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TotalTokensSpent",
                table: "Users",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalTokensEarned",
                table: "Users",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TokenBalance",
                table: "Users",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<bool>(
                name: "InitialTokensGranted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.CreateTable(
                name: "TokenTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenTransactions_Users_UserId",
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
                values: new object[] { new DateTime(2025, 9, 3, 3, 21, 54, 815, DateTimeKind.Utc).AddTicks(9242), new DateTime(2025, 9, 3, 3, 21, 54, 815, DateTimeKind.Utc).AddTicks(9242) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 3, 21, 54, 815, DateTimeKind.Utc).AddTicks(9304), new DateTime(2025, 9, 3, 3, 21, 54, 815, DateTimeKind.Utc).AddTicks(9304) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 3, 21, 54, 815, DateTimeKind.Utc).AddTicks(9321), new DateTime(2025, 9, 3, 3, 21, 54, 815, DateTimeKind.Utc).AddTicks(9321) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 3, 21, 54, 815, DateTimeKind.Utc).AddTicks(9323), new DateTime(2025, 9, 3, 3, 21, 54, 815, DateTimeKind.Utc).AddTicks(9323) });

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_RelatedEntityType_RelatedEntityId",
                table: "TokenTransactions",
                columns: new[] { "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_Source",
                table: "TokenTransactions",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_TransactionId",
                table: "TokenTransactions",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_Type",
                table: "TokenTransactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_UserId",
                table: "TokenTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_UserId_CreatedAt",
                table: "TokenTransactions",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenTransactions");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalTokensSpent",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalTokensEarned",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "TokenBalance",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<bool>(
                name: "InitialTokensGranted",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

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
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 3, 20, 58, 519, DateTimeKind.Utc).AddTicks(5812), new DateTime(2025, 9, 3, 3, 20, 58, 519, DateTimeKind.Utc).AddTicks(5812) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 3, 20, 58, 519, DateTimeKind.Utc).AddTicks(5814), new DateTime(2025, 9, 3, 3, 20, 58, 519, DateTimeKind.Utc).AddTicks(5814) });
        }
    }
}
