using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Persistence.EfCore
{
    /// <inheritdoc />
    public partial class AddUIDToUser : Migration
    {
        /// <summary>
        /// 데이터베이스 스키마와 시드 데이터를 적용한다.
        /// </summary>
        /// <remarks>
        /// - Users 테이블에 non-null string 열 `UID`(기본값: 빈 문자열)를 추가합니다.
        /// - Characters 테이블의 네 개 행(Ids: 11111111-..., 22222222-..., 33333333-..., 44444444-...)에 대해 CreatedAt 및 UpdatedAt 타임스탬프를 갱신합니다.
        /// - Users 테이블의 두 행(Ids: aaaaaaaa-..., bbbbbbbb-...)에 대해 CreatedAt, UpdatedAt, Name을 갱신하고 `UID`를 빈 문자열로 설정합니다.
        /// </remarks>
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

        /// <summary>
        /// 마이그레이션을 롤백합니다: Users 테이블에서 'UID' 열을 제거하고 변경된 시드 데이터를 이전 상태로 복원합니다.
        /// </summary>
        /// <remarks>
        /// 다음 변경을 되돌립니다:
        /// - Users 테이블: 'UID' 열 삭제 및 두 사용자 레코드(IDs aaaaaaaa-... 및 bbbbbbbb-...)의 Name·CreatedAt·UpdatedAt 값을 이전 값으로 복원.
        /// - Characters 테이블: 네 개의 특정 행(IDs 1111..., 2222..., 3333..., 4444...)에 대한 CreatedAt·UpdatedAt 값을 이전 타임스탬프로 복원.
        /// </remarks>
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
