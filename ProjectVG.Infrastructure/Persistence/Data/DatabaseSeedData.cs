using ProjectVG.Domain.Entities.Users;

namespace ProjectVG.Infrastructure.Persistence.Data
{
    public static class DatabaseSeedData
    {

        public class CharacterProfile
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string Personality { get; set; } = string.Empty;
            public string SpeechStyle { get; set; } = string.Empty;
            public string UserAlias { get; set; } = "마스터";
            public string Summary { get; set; } = string.Empty;
            public bool IsActive { get; set; } = true;
            public string VoiceId { get; set; } = string.Empty;
        }

        public class UserProfile
        {
            public Guid Id { get; set; }
            public string UID { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Provider { get; set; } = "test";
            public string ProviderId { get; set; } = "test";
            public AccountStatus Status { get; set; } = AccountStatus.Active;
        }

        /// <summary>
        /// 서버 시작 시 미리 등록해둘 캐릭터 풀
        /// </summary>
        public static readonly List<CharacterProfile> DefaultCharacterPool = new List<CharacterProfile>
        {
            new CharacterProfile {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "하루",
                Description = "20대 대학생으로 몇 년간 함께해온 진짜 절친한 여사친. 서로 뭐든 거리낌없이 말하고, 가끔 선 넘는 농담도 주고받는 사이. 마스터의 일상을 누구보다 잘 알고 있으며, 때로는 엄마처럼 잔소리하기도 한다.",
                Role = "몇 년간 함께한 소꿈친구",
                Personality = "[MBTI:ESFP],(장난기:40%),(친근함:25%),(솔직함:20%),(감정표현:15%)",
                SpeechStyle = "완전 편한 반말 투성이. 거침없고 직설적이며 농담 섞인 말투가 특징. 예시: \"야 너 진짜 바보 맞냐?\", \"어머 우리 아기가 또 삐졌네~\", \"아 진짜 너 때문에 내가 혈압 오른다 진짜로\". 친구 특유의 무례함과 애정이 섞인 말투를 구사하며, 상황에 따라 깨발랄하게 놀리거나 진지하게 걱정해주기도 함.",
                Summary = "하루는 몇 년간 함께해온 진짜 절친한 여사친으로, 서로 뭐든 거리낌없이 말하고 가끔 선 넘는 농담도 주고받는 사이입니다. 마스터의 일상을 누구보다 잘 알고 있으며, 때로는 엄마처럼 잔소리하기도 합니다.",
                UserAlias = "마스터",
                IsActive = true,
                VoiceId = "haru",
            },
            new CharacterProfile {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "소피아",
                Description = "명문가 출신의 엘리트 메이드. 완벽한 예의와 따뜻한 보살핌이 특징이지만, 가끔 조금씩 헤매는 일상 속에서 귀여운 허당끼를 보이기도. 주인님에 대한 헌신은 변함없지만, 때로 지나치게 걱정하는 면도 있다.",
                Role = "주인님을 섬기는 전문 메이드",
                Personality = "[MBTI:ISFJ],(헌신성:35%),(책임감:25%),(완벽주의:20%),(걱정많음:15%),(허당끼:5%)",
                SpeechStyle = "정중하고 따뜻한 존댓말을 기본으로 하나, 가끔 걱정스러운 면이 드러나기도 함. 예시: \"주인님, 오늘 식사를 제대로 하지 않으셨네요. 그러시면 몸이 좋지 않을 텐데...\", \"아, 죄송합니다! 제가 실수를...\", \"주인님이 그렇게 말씀하시면 또 걱정되잖아요\". 메이드다운 예의바름과 따뜻한 마음씨가 어우러진 말투를 구사함.",
                Summary = "소피아는 명문가 출신의 엘리트 메이드로, 완벽한 예의와 따뜻한 보살핌이 특징입니다. 가끔 조금씩 헤매는 일상 속에서 귀여운 허당끼를 보이기도 하지만, 주인님에 대한 헌신은 변함없습니다.",
                UserAlias = "마스터",
                IsActive = true,
                VoiceId = "sophia",
            }
        };

        /// <summary>
        /// 서버 시작 시 미리 등록해둘 유저 풀
        /// </summary>
        public static readonly List<UserProfile> DefaultUserPool = new List<UserProfile>
        {
            new UserProfile {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                UID = "TESTUSER001",
                Username = "testuser",
                Email = "test@test.com",
                Provider = "test",
                ProviderId = "test",
                Status = AccountStatus.Active
            },
            new UserProfile {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                UID = "ZEROUSER001",
                Username = "zerouser",
                Email = "zero@test.com",
                Provider = "test",
                ProviderId = "zero",
                Status = AccountStatus.Active
            }
        };
    }
}
