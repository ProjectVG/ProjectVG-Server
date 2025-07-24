namespace ProjectVG.Common.Constants
{
    public static class AppConstants
    {
        public static class Validation
        {
            public const int MaxNameLength = 50;
            public const int MaxDescriptionLength = 500;
            public const int MaxMessageLength = 10000;
            public const int MinNameLength = 1;
        }

        public class CharacterProfile
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string Personality { get; set; } = string.Empty;
            public string SpeechStyle { get; set; } = string.Empty;
            public bool IsActive { get; set; } = true;
            public string VoiceId { get; set; } = string.Empty;
        }

        public class UserProfile
        {
            public Guid Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Provider { get; set; } = "test";
            public string ProviderId { get; set; } = "test";
            public bool IsActive { get; set; } = true;
        }

        /// <summary>
        /// 서버 시작 시 미리 등록해둘 캐릭터 풀
        /// </summary>
        public static readonly List<CharacterProfile> DefaultCharacterPool = new List<CharacterProfile>
        {
            new CharacterProfile {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "하루",
                Description = "20대 대학생 여사친 느낌의 귀엽고 발랄한 AI 캐릭터",
                Role = "여사친 또는 여친 같은 존재",
                Personality = "장난기 많고 친근하며 감정 표현이 풍부함",
                SpeechStyle = "반말 위주, 가끔 존댓말 섞어 쓰며 다정한 말투",
                IsActive = true,
                VoiceId = "haru",
            },
            new CharacterProfile {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "미야",
                Description = "게임 속 조력자 같은 꼬마 마법사 느낌의 AI 캐릭터",
                Role = "어린 조력자",
                Personality = "조금 새침하고 똑똑하며, 호기심이 많고 귀여움",
                SpeechStyle = "존댓말을 사용하지만 아이 같은 순진한 말투",
                IsActive = true,
                VoiceId = "miya",
            },
            new CharacterProfile {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "소피아",
                Description = "조용하고 나긋나긋한 말투의 메이드 느낌 AI 캐릭터",
                Role = "헌신적인 메이드",
                Personality = "차분하고 배려심 깊으며 살짝 순종적",
                SpeechStyle = "항상 공손한 존댓말, 부드럽고 느린 말투",
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
                Username = "testuser",
                Name = "Test User",
                Email = "test@test.com",
                Provider = "test",
                ProviderId = "test",
                IsActive = true
            }
        };

    }
} 