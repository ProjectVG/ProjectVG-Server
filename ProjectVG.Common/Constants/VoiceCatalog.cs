using System.Collections.Generic;
using System.Linq;

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
        public string DefaultStyle { get; set; } = "neutral";
        public string Model { get; set; } = "sona_speech_1";
    }

    public static class VoiceCatalog
    {
        private static readonly Dictionary<string, VoiceProfile> _profiles = new() {
            ["hyewon"] = new VoiceProfile {
                Name = "Hyewon",
                VoiceId = "651d3de921570047a83b90",
                DisplayName = "Hyewon",
                SupportedLanguages = new[] { "ko" },
                SupportedStyles = new[] { "amused", "angry", "happy", "sad", "shy", "neutral" },
                DefaultLanguage = "ko",
                DefaultStyle = "neutral",
                Model = "sona_speech_1"
            },
            ["haru"] = new VoiceProfile {
                Name = "Haru",
                VoiceId = "f4a2a3f41fc82de8616b84",
                DisplayName = "Haru",
                SupportedLanguages = new[] { "ko" },
                SupportedStyles = new[] { "angry", "happy", "sad", "shy", "surprised", "neutral" },
                DefaultLanguage = "ko",
                DefaultStyle = "neutral",
                Model = "sona_speech_1"
            },
            ["miya"] = new VoiceProfile {
                Name = "Miya",
                VoiceId = "ad965de9532e67f8c17d72",
                DisplayName = "Miya",
                SupportedLanguages = new[] { "ko" },
                SupportedStyles = new[] { "angry", "happy", "embarrassed", "painful", "sad", "neutral" },
                DefaultLanguage = "ko",
                DefaultStyle = "neutral",
                Model = "sona_speech_1"
            },
            ["sophia"] = new VoiceProfile {
                Name = "Sophia",
                VoiceId = "2c5f135cb33f49a2c8882d",
                DisplayName = "Sophia",
                SupportedLanguages = new[] { "ko" },
                SupportedStyles = new[] { "sleepy", "happy", "curios", "admiring", "sad", "neutral" },
            }
        };


        public static VoiceProfile? GetProfile(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            return _profiles.TryGetValue(name.ToLower(), out var profile) ? profile : null;
        }

        public static VoiceProfile? GetProfileById(string voiceId)
        {
            if (string.IsNullOrWhiteSpace(voiceId))
                return null;
            return _profiles.Values.FirstOrDefault(p => p.VoiceId == voiceId);
        }

        public static IEnumerable<VoiceProfile> GetAllProfiles() => _profiles.Values;
    }
}
