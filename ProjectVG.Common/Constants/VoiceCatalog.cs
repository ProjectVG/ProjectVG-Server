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
            ["Hyewon"] = new VoiceProfile
            {
                Name = "Hyewon",
                VoiceId = "651d3de921570047a83b90",
                DisplayName = "Hyewon",
                SupportedLanguages = new[] { "ko" },
                SupportedStyles = new[] { "Amused", "Angry", "Happay", "Sad", "Shy", "Neutral" },
                DefaultLanguage = "ko",
                DefaultStyle = "Neutral",
                Model = "sona_speech_1"
            },
            ["Haru"] = new VoiceProfile
            {
                Name = "Haru",
                VoiceId = "f4a2a3f41fc82de8616b84",
                DisplayName = "Haru",
                SupportedLanguages = new[] { "ko" },
                SupportedStyles = new[] { "Angry", "Happay", "Sad", "Shy", "Surprised", "Neutral" },
                DefaultLanguage = "ko",
                DefaultStyle = "Neutral",
                Model = "sona_speech_1"
            },
            ["Miya"] = new VoiceProfile
            {
                Name = "Miya",
                VoiceId = "ad965de9532e67f8c17d72",
                DisplayName = "Miya",
                SupportedLanguages = new[] { "ko" },
                SupportedStyles = new[] { "Angry", "Happy", "Embarrassed", "Painful", "Sad", "Neutral" },
                DefaultLanguage = "ko",
                DefaultStyle = "Neutral",
                Model = "sona_speech_1"
            }
        };

        public static VoiceProfile? GetProfile(string name)
            => _profiles.TryGetValue(name, out var profile) ? profile : null;

        public static IEnumerable<VoiceProfile> GetAllProfiles() => _profiles.Values;
    }
}
