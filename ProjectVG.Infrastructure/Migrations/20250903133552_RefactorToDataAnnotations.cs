using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorToDataAnnotations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ProviderId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_UID",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_CreditTransactions_Source",
                table: "CreditTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CreditTransactions_TransactionId",
                table: "CreditTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CreditTransactions_Type",
                table: "CreditTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CreditTransactions_UserId",
                table: "CreditTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ConversationHistories_ConversationId",
                table: "ConversationHistories");

            migrationBuilder.DropIndex(
                name: "IX_ConversationHistories_Role",
                table: "ConversationHistories");

            migrationBuilder.DropIndex(
                name: "IX_ConversationHistories_Timestamp",
                table: "ConversationHistories");

            migrationBuilder.DropIndex(
                name: "IX_ConversationHistories_UserId",
                table: "ConversationHistories");

            migrationBuilder.DropIndex(
                name: "IX_Characters_ConfigMode",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_IsActive",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_IsPublic",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_Name",
                table: "Characters");

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 13, 35, 52, 398, DateTimeKind.Utc).AddTicks(1063), new DateTime(2025, 9, 3, 13, 35, 52, 398, DateTimeKind.Utc).AddTicks(1063) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 13, 35, 52, 398, DateTimeKind.Utc).AddTicks(1125), new DateTime(2025, 9, 3, 13, 35, 52, 398, DateTimeKind.Utc).AddTicks(1125) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 13, 35, 52, 398, DateTimeKind.Utc).AddTicks(1147), new DateTime(2025, 9, 3, 13, 35, 52, 398, DateTimeKind.Utc).AddTicks(1147) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 3, 13, 35, 52, 398, DateTimeKind.Utc).AddTicks(1149), new DateTime(2025, 9, 3, 13, 35, 52, 398, DateTimeKind.Utc).AddTicks(1149) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProviderId",
                table: "Users",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UID",
                table: "Users",
                column: "UID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Source",
                table: "CreditTransactions",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_TransactionId",
                table: "CreditTransactions",
                column: "TransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_Type",
                table: "CreditTransactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransactions_UserId",
                table: "CreditTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationHistories_ConversationId",
                table: "ConversationHistories",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationHistories_Role",
                table: "ConversationHistories",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationHistories_Timestamp",
                table: "ConversationHistories",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationHistories_UserId",
                table: "ConversationHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_ConfigMode",
                table: "Characters",
                column: "ConfigMode");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_IsActive",
                table: "Characters",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_IsPublic",
                table: "Characters",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Name",
                table: "Characters",
                column: "Name");
        }
    }
}
