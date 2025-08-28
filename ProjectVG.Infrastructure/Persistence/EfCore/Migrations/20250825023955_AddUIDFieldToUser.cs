using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Persistence.EfCore
{
    /// <inheritdoc />
    public partial class AddUIDFieldToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8922), new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8922) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8925), new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8925) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8927), new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8927) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8929), new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8929) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8942), new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8943) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8944), new DateTime(2025, 8, 25, 2, 39, 55, 748, DateTimeKind.Utc).AddTicks(8945) });
        }

        /// <summary>
        /// 이 마이그레이션의 적용을 되돌립니다.
        /// </summary>
        /// <remarks>
        /// Seed 데이터의 CreatedAt 및 UpdatedAt 값을 이전 타임스탬프로 복원합니다.
        /// 복원 대상: Characters 테이블(IDs: 11111111-1111-1111-1111-111111111111, 22222222-2222-2222-2222-222222222222, 33333333-3333-3333-3333-333333333333, 44444444-4444-4444-4444-444444444444) 및
        /// Users 테이블(IDs: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa, bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb).
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
