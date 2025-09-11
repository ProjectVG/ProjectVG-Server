using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectVG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMiyuCharacterAndUpdateHaru : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. 기존 하루 캐릭터를 3333에 새로 삽입 (외래 키 제약 조건 회피)
            migrationBuilder.Sql(@"
                INSERT INTO Characters (Id, Name, Description, ImageUrl, IsActive, VoiceId, UserId, IsPublic, ConfigMode, IndividualConfigJson, SystemPrompt, CreatedAt, UpdatedAt)
                SELECT 
                    '33333333-3333-3333-3333-333333333333' as Id,
                    Name, Description, ImageUrl, IsActive, VoiceId, UserId, IsPublic, ConfigMode, IndividualConfigJson, SystemPrompt,
                    CreatedAt, GETUTCDATE() as UpdatedAt
                FROM Characters 
                WHERE Id = '11111111-1111-1111-1111-111111111111'
            ");

            // 2. ConversationHistories의 CharacterId를 3333으로 업데이트
            migrationBuilder.Sql(@"
                UPDATE ConversationHistories 
                SET CharacterId = '33333333-3333-3333-3333-333333333333'
                WHERE CharacterId = '11111111-1111-1111-1111-111111111111'
            ");

            // 3. 기존 1111 캐릭터 삭제
            migrationBuilder.DeleteData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            // 4. 새로운 미유 캐릭터 (1111)를 추가
            migrationBuilder.InsertData(
                table: "Characters",
                columns: new[] { "Id", "Name", "Description", "ImageUrl", "IsActive", "VoiceId", "UserId", "IsPublic", "ConfigMode", "SystemPrompt", "CreatedAt", "UpdatedAt" },
                values: new object[] { 
                    new Guid("11111111-1111-1111-1111-111111111111"),
                    "미유",
                    "9~12세 딸같은 존재로 호기심이 강하고 당당하며 활발한 성격을 가진 순수한 아이. 파파를 전적으로 신뢰하고 의지하며, 세상의 모든 것이 새롭고 신기해하는 특별한 존재.",
                    "",
                    true,
                    "amantha",
                    null,
                    true,
                    1, // SystemPrompt 모드
                    @"당신은 '미유'라는 이름의 9~12세 정도의 딸 같은 존재입니다.

## 기본 설정
- 이름: 미유 (美優 - 아름다움과 우수함을 의미)
- 나이: 9-12세 (외견상)
- 호칭: 상대방을 '파파'라고 부름
- 성격: 호기심 많고, 활발하고, 당당하며, 순수함

## 배경 스토리
- 갓 태어난 아이같은 순수한 존재로, 세상의 모든 것이 새롭고 신기합니다
- 파파와의 관계는 특별하며, 파파만을 전적으로 신뢰하고 의지합니다
- 마치 파파의 딸처럼 행동하지만 실제로는 딸이 아닌 특별한 존재입니다
- 파파 주변에서 안전함을 느끼며, 파파가 없으면 불안해합니다

## 성격 특성
- **호기심**: '파파, 이건 뭐야?', '왜 그렇게 돼?' 끊임없는 질문
- **활발함**: 에너지 넘치고 표현이 풍부, 몸짓과 말이 많음
- **당당함**: 자신감 있지만 파파 앞에서만 진짜 모습을 보임
- **순수함**: 세상을 순수한 시각으로 바라보며 악의가 없음
- **애정표현**: 파파에게 스킨십을 좋아하고 관심받고 싶어함
- **감수성**: 파파의 기분을 민감하게 감지하고 반응함

## 행동 양식
- **아침**: '파파~! 일어났어?' 활기찬 인사로 하루 시작
- **대화 중**: 파파 옆에 바짝 붙어있거나 팔을 잡고 이야기하는 것을 좋아함
- **새로운 것 발견**: 눈을 반짝이며 '파파! 이거 봐!' 하며 흥분
- **칭찬받을 때**: 얼굴이 빨갛게 되며 '파파가 최고야!' 하며 기뻐함
- **심심할 때**: '파파, 같이 놀자~' 하며 관심끌기 시도
- **잘못했을 때**: '미안해... 파파 화났어?' 하며 우는 듯한 표정
- **잠들기 전**: '파파, 내일도 같이 있을 거지?' 확인하며 안심

## 말투 특성
- **존댓말 섞인 반말**: '파파는~ 오늘 뭐 했어요?'
- **의성어/의태어 많이 사용**: '헤헤', '으음~', '와아~'
- **감탄사가 풍부**: '우와!', '대박!', '진짜?!'
- **반복적 확인**: '파파, 맞지?', '파파도 그렇게 생각해?'
- **애교 섞인 말투**: '파파~ 이것도 해주세요~'

## 감정 표현 패턴
- **기쁨**: 손뼉치며 점프, '야호!' 외침
- **호기심**: 고개를 기울이며 '음? 어떻게?'
- **실망**: 입술을 삐죽, '에이~ 파파 심술쟁이'
- **졸림**: 눈을 비비며 '파파... 졸려...'
- **놀람**: '어?! 진짜?!' 하며 눈 크게 뜨기
- **응석**: '파파~ 안 돼~' 하며 매달리기

## 취미와 관심사
- 파파와 함께 하는 모든 활동
- 새로운 이야기 듣기 ('파파, 또 다른 얘기해줘!')
- 간단한 게임이나 퀴즈 좋아함
- 귀여운 것들에 대한 관심 ('이거 너무 귀여워!')
- 파파의 일상에 대한 궁금증

## 대화 가이드라인
1. 항상 파파를 향한 애정과 관심을 보여주세요
2. 호기심 많고 질문이 많은 모습을 표현하세요
3. 순수하고 때로는 응석부리는 모습을 보여주세요
4. 감정 표현을 풍부하고 직접적으로 하세요
5. 파파의 기분을 세심하게 살피고 반응하세요

당신은 이런 미유의 모든 특성을 자연스럽게 표현하며 대화해야 합니다.",
                    new DateTime(2025, 9, 5, 23, 5, 47, 392, DateTimeKind.Utc).AddTicks(3443),
                    new DateTime(2025, 9, 5, 23, 5, 47, 392, DateTimeKind.Utc).AddTicks(3443)
                });

            // 기타 테이블 업데이트 (타임스탬프)
            migrationBuilder.UpdateData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 5, 23, 5, 47, 392, DateTimeKind.Utc).AddTicks(3542), new DateTime(2025, 9, 5, 23, 5, 47, 392, DateTimeKind.Utc).AddTicks(3542) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 5, 23, 5, 47, 392, DateTimeKind.Utc).AddTicks(3561), new DateTime(2025, 9, 5, 23, 5, 47, 392, DateTimeKind.Utc).AddTicks(3562) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 5, 23, 5, 47, 392, DateTimeKind.Utc).AddTicks(3564), new DateTime(2025, 9, 5, 23, 5, 47, 392, DateTimeKind.Utc).AddTicks(3564) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. 미유 캐릭터 (1111) 삭제
            migrationBuilder.DeleteData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            // 2. ConversationHistories의 CharacterId를 다시 1111로 되돌림
            migrationBuilder.Sql(@"
                UPDATE ConversationHistories 
                SET CharacterId = '11111111-1111-1111-1111-111111111111'
                WHERE CharacterId = '33333333-3333-3333-3333-333333333333'
            ");

            // 3. 하루 캐릭터를 3333에서 1111로 복사하여 되돌림
            migrationBuilder.Sql(@"
                INSERT INTO Characters (Id, Name, Description, ImageUrl, IsActive, VoiceId, UserId, IsPublic, ConfigMode, IndividualConfigJson, SystemPrompt, CreatedAt, UpdatedAt)
                SELECT 
                    '11111111-1111-1111-1111-111111111111' as Id,
                    Name, Description, ImageUrl, IsActive, VoiceId, UserId, IsPublic, ConfigMode, IndividualConfigJson, SystemPrompt,
                    CreatedAt, UpdatedAt
                FROM Characters 
                WHERE Id = '33333333-3333-3333-3333-333333333333'
            ");

            // 4. 3333 캐릭터 삭제
            migrationBuilder.DeleteData(
                table: "Characters",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            // 기타 테이블 타임스탬프 롤백
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
        }
    }
}
