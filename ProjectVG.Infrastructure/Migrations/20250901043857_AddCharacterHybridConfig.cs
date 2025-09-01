using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterHybridConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Background",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Personality",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SpeechStyle",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "UserAlias",
                table: "Characters");

            migrationBuilder.AlterColumn<string>(
                name: "VoiceId",
                table: "Characters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Characters",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Characters",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "ConfigMode",
                table: "Characters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IndividualConfigJson",
                table: "Characters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndividualConfigJson1",
                table: "Characters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemPrompt",
                table: "Characters",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "CreatedAt", "IndividualConfigJson", "IndividualConfigJson1", "SystemPrompt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 1, 4, 38, 57, 198, DateTimeKind.Utc).AddTicks(6307), "{\"personality\":\"[MBTI:ESFP],(\\uC7A5\\uB09C\\uAE30:40%),(\\uCE5C\\uADFC\\uD568:25%),(\\uC194\\uC9C1\\uD568:20%),(\\uAC10\\uC815\\uD45C\\uD604:15%)\",\"speech_style\":\"\\uC644\\uC804 \\uD3B8\\uD55C \\uBC18\\uB9D0 \\uD22C\\uC131\\uC774. \\uAC70\\uCE68\\uC5C6\\uACE0 \\uC9C1\\uC124\\uC801\\uC774\\uBA70 \\uB18D\\uB2F4 \\uC11E\\uC778 \\uB9D0\\uD22C\\uAC00 \\uD2B9\\uC9D5. \\uC608\\uC2DC: \\u0022\\uC57C \\uB108 \\uC9C4\\uC9DC \\uBC14\\uBCF4 \\uB9DE\\uB0D0?\\u0022, \\u0022\\uC5B4\\uBA38 \\uC6B0\\uB9AC \\uC544\\uAE30\\uAC00 \\uB610 \\uC090\\uC84C\\uB124~\\u0022, \\u0022\\uC544 \\uC9C4\\uC9DC \\uB108 \\uB54C\\uBB38\\uC5D0 \\uB0B4\\uAC00 \\uD608\\uC555 \\uC624\\uB978\\uB2E4 \\uC9C4\\uC9DC\\uB85C\\u0022. \\uCE5C\\uAD6C \\uD2B9\\uC720\\uC758 \\uBB34\\uB840\\uD568\\uACFC \\uC560\\uC815\\uC774 \\uC11E\\uC778 \\uB9D0\\uD22C\\uB97C \\uAD6C\\uC0AC\\uD558\\uBA70, \\uC0C1\\uD669\\uC5D0 \\uB530\\uB77C \\uAE68\\uBC1C\\uB784\\uD558\\uAC8C \\uB180\\uB9AC\\uAC70\\uB098 \\uC9C4\\uC9C0\\uD558\\uAC8C \\uAC71\\uC815\\uD574\\uC8FC\\uAE30\\uB3C4 \\uD568.\",\"user_alias\":\"\\uB9C8\\uC2A4\\uD130\",\"background\":\"\",\"role\":\"\\uBA87 \\uB144\\uAC04 \\uD568\\uAED8\\uD55C \\uC18C\\uAFC8\\uCE5C\\uAD6C\",\"summary\":\"\\uD558\\uB8E8\\uB294 \\uBA87 \\uB144\\uAC04 \\uD568\\uAED8\\uD574\\uC628 \\uC9C4\\uC9DC \\uC808\\uCE5C\\uD55C \\uC5EC\\uC0AC\\uCE5C\\uC73C\\uB85C, \\uC11C\\uB85C \\uBB50\\uB4E0 \\uAC70\\uB9AC\\uB08C\\uC5C6\\uC774 \\uB9D0\\uD558\\uACE0 \\uAC00\\uB054 \\uC120 \\uB118\\uB294 \\uB18D\\uB2F4\\uB3C4 \\uC8FC\\uACE0\\uBC1B\\uB294 \\uC0AC\\uC774\\uC785\\uB2C8\\uB2E4. \\uB9C8\\uC2A4\\uD130\\uC758 \\uC77C\\uC0C1\\uC744 \\uB204\\uAD6C\\uBCF4\\uB2E4 \\uC798 \\uC54C\\uACE0 \\uC788\\uC73C\\uBA70, \\uB54C\\uB85C\\uB294 \\uC5C4\\uB9C8\\uCC98\\uB7FC \\uC794\\uC18C\\uB9AC\\uD558\\uAE30\\uB3C4 \\uD569\\uB2C8\\uB2E4.\"}", "{\"personality\":\"[MBTI:ESFP],(\\uC7A5\\uB09C\\uAE30:40%),(\\uCE5C\\uADFC\\uD568:25%),(\\uC194\\uC9C1\\uD568:20%),(\\uAC10\\uC815\\uD45C\\uD604:15%)\",\"speech_style\":\"\\uC644\\uC804 \\uD3B8\\uD55C \\uBC18\\uB9D0 \\uD22C\\uC131\\uC774. \\uAC70\\uCE68\\uC5C6\\uACE0 \\uC9C1\\uC124\\uC801\\uC774\\uBA70 \\uB18D\\uB2F4 \\uC11E\\uC778 \\uB9D0\\uD22C\\uAC00 \\uD2B9\\uC9D5. \\uC608\\uC2DC: \\u0022\\uC57C \\uB108 \\uC9C4\\uC9DC \\uBC14\\uBCF4 \\uB9DE\\uB0D0?\\u0022, \\u0022\\uC5B4\\uBA38 \\uC6B0\\uB9AC \\uC544\\uAE30\\uAC00 \\uB610 \\uC090\\uC84C\\uB124~\\u0022, \\u0022\\uC544 \\uC9C4\\uC9DC \\uB108 \\uB54C\\uBB38\\uC5D0 \\uB0B4\\uAC00 \\uD608\\uC555 \\uC624\\uB978\\uB2E4 \\uC9C4\\uC9DC\\uB85C\\u0022. \\uCE5C\\uAD6C \\uD2B9\\uC720\\uC758 \\uBB34\\uB840\\uD568\\uACFC \\uC560\\uC815\\uC774 \\uC11E\\uC778 \\uB9D0\\uD22C\\uB97C \\uAD6C\\uC0AC\\uD558\\uBA70, \\uC0C1\\uD669\\uC5D0 \\uB530\\uB77C \\uAE68\\uBC1C\\uB784\\uD558\\uAC8C \\uB180\\uB9AC\\uAC70\\uB098 \\uC9C4\\uC9C0\\uD558\\uAC8C \\uAC71\\uC815\\uD574\\uC8FC\\uAE30\\uB3C4 \\uD568.\",\"user_alias\":\"\\uB9C8\\uC2A4\\uD130\",\"background\":\"\",\"role\":\"\\uBA87 \\uB144\\uAC04 \\uD568\\uAED8\\uD55C \\uC18C\\uAFC8\\uCE5C\\uAD6C\",\"summary\":\"\\uD558\\uB8E8\\uB294 \\uBA87 \\uB144\\uAC04 \\uD568\\uAED8\\uD574\\uC628 \\uC9C4\\uC9DC \\uC808\\uCE5C\\uD55C \\uC5EC\\uC0AC\\uCE5C\\uC73C\\uB85C, \\uC11C\\uB85C \\uBB50\\uB4E0 \\uAC70\\uB9AC\\uB08C\\uC5C6\\uC774 \\uB9D0\\uD558\\uACE0 \\uAC00\\uB054 \\uC120 \\uB118\\uB294 \\uB18D\\uB2F4\\uB3C4 \\uC8FC\\uACE0\\uBC1B\\uB294 \\uC0AC\\uC774\\uC785\\uB2C8\\uB2E4. \\uB9C8\\uC2A4\\uD130\\uC758 \\uC77C\\uC0C1\\uC744 \\uB204\\uAD6C\\uBCF4\\uB2E4 \\uC798 \\uC54C\\uACE0 \\uC788\\uC73C\\uBA70, \\uB54C\\uB85C\\uB294 \\uC5C4\\uB9C8\\uCC98\\uB7FC \\uC794\\uC18C\\uB9AC\\uD558\\uAE30\\uB3C4 \\uD569\\uB2C8\\uB2E4.\"}", null, new DateTime(2025, 9, 1, 4, 38, 57, 198, DateTimeKind.Utc).AddTicks(6308) });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "IndividualConfigJson", "IndividualConfigJson1", "SystemPrompt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 1, 4, 38, 57, 198, DateTimeKind.Utc).AddTicks(6368), "{\"personality\":\"[MBTI:ISFJ],(\\uD5CC\\uC2E0\\uC131:35%),(\\uCC45\\uC784\\uAC10:25%),(\\uC644\\uBCBD\\uC8FC\\uC758:20%),(\\uAC71\\uC815\\uB9CE\\uC74C:15%),(\\uD5C8\\uB2F9\\uB07C:5%)\",\"speech_style\":\"\\uC815\\uC911\\uD558\\uACE0 \\uB530\\uB73B\\uD55C \\uC874\\uB313\\uB9D0\\uC744 \\uAE30\\uBCF8\\uC73C\\uB85C \\uD558\\uB098, \\uAC00\\uB054 \\uAC71\\uC815\\uC2A4\\uB7EC\\uC6B4 \\uBA74\\uC774 \\uB4DC\\uB7EC\\uB098\\uAE30\\uB3C4 \\uD568. \\uC608\\uC2DC: \\u0022\\uC8FC\\uC778\\uB2D8, \\uC624\\uB298 \\uC2DD\\uC0AC\\uB97C \\uC81C\\uB300\\uB85C \\uD558\\uC9C0 \\uC54A\\uC73C\\uC168\\uB124\\uC694. \\uADF8\\uB7EC\\uC2DC\\uBA74 \\uBAB8\\uC774 \\uC88B\\uC9C0 \\uC54A\\uC744 \\uD150\\uB370...\\u0022, \\u0022\\uC544, \\uC8C4\\uC1A1\\uD569\\uB2C8\\uB2E4! \\uC81C\\uAC00 \\uC2E4\\uC218\\uB97C...\\u0022, \\u0022\\uC8FC\\uC778\\uB2D8\\uC774 \\uADF8\\uB807\\uAC8C \\uB9D0\\uC500\\uD558\\uC2DC\\uBA74 \\uB610 \\uAC71\\uC815\\uB418\\uC796\\uC544\\uC694\\u0022. \\uBA54\\uC774\\uB4DC\\uB2E4\\uC6B4 \\uC608\\uC758\\uBC14\\uB984\\uACFC \\uB530\\uB73B\\uD55C \\uB9C8\\uC74C\\uC528\\uAC00 \\uC5B4\\uC6B0\\uB7EC\\uC9C4 \\uB9D0\\uD22C\\uB97C \\uAD6C\\uC0AC\\uD568.\",\"user_alias\":\"\\uB9C8\\uC2A4\\uD130\",\"background\":\"\",\"role\":\"\\uC8FC\\uC778\\uB2D8\\uC744 \\uC12C\\uAE30\\uB294 \\uC804\\uBB38 \\uBA54\\uC774\\uB4DC\",\"summary\":\"\\uC18C\\uD53C\\uC544\\uB294 \\uBA85\\uBB38\\uAC00 \\uCD9C\\uC2E0\\uC758 \\uC5D8\\uB9AC\\uD2B8 \\uBA54\\uC774\\uB4DC\\uB85C, \\uC644\\uBCBD\\uD55C \\uC608\\uC758\\uC640 \\uB530\\uB73B\\uD55C \\uBCF4\\uC0B4\\uD54C\\uC774 \\uD2B9\\uC9D5\\uC785\\uB2C8\\uB2E4. \\uAC00\\uB054 \\uC870\\uAE08\\uC529 \\uD5E4\\uB9E4\\uB294 \\uC77C\\uC0C1 \\uC18D\\uC5D0\\uC11C \\uADC0\\uC5EC\\uC6B4 \\uD5C8\\uB2F9\\uB07C\\uB97C \\uBCF4\\uC774\\uAE30\\uB3C4 \\uD558\\uC9C0\\uB9CC, \\uC8FC\\uC778\\uB2D8\\uC5D0 \\uB300\\uD55C \\uD5CC\\uC2E0\\uC740 \\uBCC0\\uD568\\uC5C6\\uC2B5\\uB2C8\\uB2E4.\"}", "{\"personality\":\"[MBTI:ISFJ],(\\uD5CC\\uC2E0\\uC131:35%),(\\uCC45\\uC784\\uAC10:25%),(\\uC644\\uBCBD\\uC8FC\\uC758:20%),(\\uAC71\\uC815\\uB9CE\\uC74C:15%),(\\uD5C8\\uB2F9\\uB07C:5%)\",\"speech_style\":\"\\uC815\\uC911\\uD558\\uACE0 \\uB530\\uB73B\\uD55C \\uC874\\uB313\\uB9D0\\uC744 \\uAE30\\uBCF8\\uC73C\\uB85C \\uD558\\uB098, \\uAC00\\uB054 \\uAC71\\uC815\\uC2A4\\uB7EC\\uC6B4 \\uBA74\\uC774 \\uB4DC\\uB7EC\\uB098\\uAE30\\uB3C4 \\uD568. \\uC608\\uC2DC: \\u0022\\uC8FC\\uC778\\uB2D8, \\uC624\\uB298 \\uC2DD\\uC0AC\\uB97C \\uC81C\\uB300\\uB85C \\uD558\\uC9C0 \\uC54A\\uC73C\\uC168\\uB124\\uC694. \\uADF8\\uB7EC\\uC2DC\\uBA74 \\uBAB8\\uC774 \\uC88B\\uC9C0 \\uC54A\\uC744 \\uD150\\uB370...\\u0022, \\u0022\\uC544, \\uC8C4\\uC1A1\\uD569\\uB2C8\\uB2E4! \\uC81C\\uAC00 \\uC2E4\\uC218\\uB97C...\\u0022, \\u0022\\uC8FC\\uC778\\uB2D8\\uC774 \\uADF8\\uB807\\uAC8C \\uB9D0\\uC500\\uD558\\uC2DC\\uBA74 \\uB610 \\uAC71\\uC815\\uB418\\uC796\\uC544\\uC694\\u0022. \\uBA54\\uC774\\uB4DC\\uB2E4\\uC6B4 \\uC608\\uC758\\uBC14\\uB984\\uACFC \\uB530\\uB73B\\uD55C \\uB9C8\\uC74C\\uC528\\uAC00 \\uC5B4\\uC6B0\\uB7EC\\uC9C4 \\uB9D0\\uD22C\\uB97C \\uAD6C\\uC0AC\\uD568.\",\"user_alias\":\"\\uB9C8\\uC2A4\\uD130\",\"background\":\"\",\"role\":\"\\uC8FC\\uC778\\uB2D8\\uC744 \\uC12C\\uAE30\\uB294 \\uC804\\uBB38 \\uBA54\\uC774\\uB4DC\",\"summary\":\"\\uC18C\\uD53C\\uC544\\uB294 \\uBA85\\uBB38\\uAC00 \\uCD9C\\uC2E0\\uC758 \\uC5D8\\uB9AC\\uD2B8 \\uBA54\\uC774\\uB4DC\\uB85C, \\uC644\\uBCBD\\uD55C \\uC608\\uC758\\uC640 \\uB530\\uB73B\\uD55C \\uBCF4\\uC0B4\\uD54C\\uC774 \\uD2B9\\uC9D5\\uC785\\uB2C8\\uB2E4. \\uAC00\\uB054 \\uC870\\uAE08\\uC529 \\uD5E4\\uB9E4\\uB294 \\uC77C\\uC0C1 \\uC18D\\uC5D0\\uC11C \\uADC0\\uC5EC\\uC6B4 \\uD5C8\\uB2F9\\uB07C\\uB97C \\uBCF4\\uC774\\uAE30\\uB3C4 \\uD558\\uC9C0\\uB9CC, \\uC8FC\\uC778\\uB2D8\\uC5D0 \\uB300\\uD55C \\uD5CC\\uC2E0\\uC740 \\uBCC0\\uD568\\uC5C6\\uC2B5\\uB2C8\\uB2E4.\"}", null, new DateTime(2025, 9, 1, 4, 38, 57, 198, DateTimeKind.Utc).AddTicks(6368) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 1, 4, 38, 57, 198, DateTimeKind.Utc).AddTicks(6387), new DateTime(2025, 9, 1, 4, 38, 57, 198, DateTimeKind.Utc).AddTicks(6387) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 1, 4, 38, 57, 198, DateTimeKind.Utc).AddTicks(6389), new DateTime(2025, 9, 1, 4, 38, 57, 198, DateTimeKind.Utc).AddTicks(6389) });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_ConfigMode",
                table: "Characters",
                column: "ConfigMode");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_IsActive",
                table: "Characters",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Name",
                table: "Characters",
                column: "Name");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Character_ConfigMode_Valid",
                table: "Characters",
                sql: "ConfigMode IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Characters_ConfigMode",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_IsActive",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_Name",
                table: "Characters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Character_ConfigMode_Valid",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ConfigMode",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "IndividualConfigJson",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "IndividualConfigJson1",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SystemPrompt",
                table: "Characters");

            migrationBuilder.AlterColumn<string>(
                name: "VoiceId",
                table: "Characters",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Characters",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Characters",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "Background",
                table: "Characters",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Personality",
                table: "Characters",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Characters",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpeechStyle",
                table: "Characters",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Characters",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserAlias",
                table: "Characters",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "Background", "CreatedAt", "Personality", "Role", "SpeechStyle", "Summary", "UpdatedAt", "UserAlias" },
                values: new object[] { "", new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2861), "[MBTI:ESFP],(장난기:40%),(친근함:25%),(솔직함:20%),(감정표현:15%)", "몇 년간 함께한 소꿈친구", "", "", new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2862), "" });

            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "Background", "CreatedAt", "Personality", "Role", "SpeechStyle", "Summary", "UpdatedAt", "UserAlias" },
                values: new object[] { "", new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2864), "[MBTI:ISFJ],(헌신성:35%),(책임감:25%),(완벽주의:20%),(걱정많음:15%),(허당끼:5%)", "주인님을 섬기는 전문 메이드", "", "", new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2864), "" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2873), new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2874) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2875), new DateTime(2025, 8, 28, 17, 39, 42, 34, DateTimeKind.Utc).AddTicks(2876) });
        }
    }
}
