using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Persistence.EfCore
{
    /// <inheritdoc />
    public partial class IncreaseUIDLength : Migration
    {
        /// <summary>
        /// 사용자 테이블의 UID 열 길이를 12자에서 16자로 확장하고 관련 시드 데이터의 타임스탬프 및 일부 사용자 상태를 갱신하는 마이그레이션을 적용합니다.
        /// </summary>
        /// <remarks>
        /// - 스키마 변경: Users 테이블의 `UID` 열을 `nvarchar(12)`(maxLength:12)에서 `nvarchar(16)`(maxLength:16, not null)로 변경합니다.
        /// - 시드 데이터 변경: 특정 Characters 행들의 `CreatedAt` 및 `UpdatedAt` 값을 갱신하고, 두 Users 행의 `CreatedAt`, `UpdatedAt` 값을 갱신하며 해당 Users의 `Status`를 0으로 설정합니다.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UID",
                table: "Users",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(12)",
                oldMaxLength: 12);

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1753), new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1753) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1756), new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1756) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1758), new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1758) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1789), new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1789) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "Status", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1801), 0, new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1801) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "Status", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1803), 0, new DateTime(2025, 8, 25, 13, 50, 4, 368, DateTimeKind.Utc).AddTicks(1803) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UID",
                table: "Users",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16);

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
                columns: new[] { "CreatedAt", "Status", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4427), 1, new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4427) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "Status", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4430), 1, new DateTime(2025, 8, 25, 5, 10, 22, 188, DateTimeKind.Utc).AddTicks(4430) });
        }
    }
}
