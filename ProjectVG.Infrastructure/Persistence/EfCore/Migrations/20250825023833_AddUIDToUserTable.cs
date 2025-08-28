using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Persistence.EfCore
{
    /// <inheritdoc />
    public partial class AddUIDToUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 38, 33, 60, DateTimeKind.Utc).AddTicks(3933), new DateTime(2025, 8, 25, 2, 38, 33, 60, DateTimeKind.Utc).AddTicks(3933) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 38, 33, 60, DateTimeKind.Utc).AddTicks(3936), new DateTime(2025, 8, 25, 2, 38, 33, 60, DateTimeKind.Utc).AddTicks(3936) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 38, 33, 60, DateTimeKind.Utc).AddTicks(3938), new DateTime(2025, 8, 25, 2, 38, 33, 60, DateTimeKind.Utc).AddTicks(3939) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 38, 33, 60, DateTimeKind.Utc).AddTicks(3940), new DateTime(2025, 8, 25, 2, 38, 33, 60, DateTimeKind.Utc).AddTicks(3941) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 38, 33, 60, DateTimeKind.Utc).AddTicks(3952), new DateTime(2025, 8, 25, 2, 38, 33, 60, DateTimeKind.Utc).AddTicks(3952) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 38, 33, 60, DateTimeKind.Utc).AddTicks(3954), new DateTime(2025, 8, 25, 2, 38, 33, 60, DateTimeKind.Utc).AddTicks(3954) });
        }

        /// <summary>
        /// 마이그레이션 적용을 되돌립니다.
        /// </summary>
        /// <remarks>
        /// Up 메서드에서 변경한 시드 데이터의 CreatedAt 및 UpdatedAt 값을 원래 타임스탬프(2025-08-25 02:36:22.883 UTC에 특정 ticks 추가된 값)로 복원합니다.
        /// 복원 대상: Characters 테이블의 4개 레코드(IDs: 11111111-..., 22222222-..., 33333333-..., 44444444-...)와 Users 테이블의 2개 레코드(IDs: aaaaaaaa-..., bbbbbbbb-...).
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5783), new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5784) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5786), new DateTime(2025, 8, 25, 2, 36, 22, 883, DateTimeKind.Utc).AddTicks(5786) });
        }
    }
}
