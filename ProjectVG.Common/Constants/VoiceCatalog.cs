using System.Collections.Generic;

namespace ProjectVG.Common.Constants
{
    public class VoiceProfile
    {
        public string Name { get; set; } = string.Empty;
        public string VoiceId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string[] SupportedLanguages { get; set; } = System.Array.Empty<string>();
        public string[] SupportedStyles { get; set; } = System.Array.Empty<string>();
        public string DefaultLanguage { get; set; } = "ko";
        public string DefaultStyle { get; set; } = "Natural";
        public string Model { get; set; } = "sona_speech_1";
    }

    public static class VoiceCatalog
    {
        private static readonly Dictionary<string, VoiceProfile> _profiles = new()
        {
            ["Zero"] = new VoiceProfile
            {
                Name = "Zero",
                VoiceId = "do1p29ed1j02",
                DisplayName = "Supertone Zero",
                SupportedLanguages = new[] { "ko", "jp", "en" },
                SupportedStyles = new[] { "Angry", "Natural", "Shy" },
                DefaultLanguage = "ko",
                DefaultStyle = "Natural",
                Model = "sona_speech_1"
            },
            ["Hana"] = new VoiceProfile
            {
                Name = "Hana",
                VoiceId = "djqwi102312dah",
                DisplayName = "Supertone Hana",
                SupportedLanguages = new[] { "ko" },
                SupportedStyles = new[] { "Sad", "Natural" },
                DefaultLanguage = "ko",
                DefaultStyle = "Natural",
                Model = "sona_speech_1"
            }
            // 추가 보이스는 여기에 계속 등록
        };

        public static VoiceProfile? GetProfile(string name)
            => _profiles.TryGetValue(name, out var profile) ? profile : null;

        public static IEnumerable<VoiceProfile> GetAllProfiles() => _profiles.Values;
    }
} 