using System.Text.Json.Serialization;

namespace ProjectVG.Application.Models.Chat
{
    public record ChatProcessResultMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = "chat";
        
        [JsonPropertyName("message_type")]
        public string MessageType { get; init; } = "json";
        
        [JsonPropertyName("text")]
        public string? Text { get; init; }
        
        [JsonPropertyName("audio_data")]
        public string? AudioData { get; init; }
        
        [JsonPropertyName("audio_format")]
        public string? AudioFormat { get; init; }
        
        [JsonPropertyName("audio_length")]
        public float? AudioLength { get; init; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; init; }
        
        public ChatProcessResultMessage WithAudioData(byte[]? audioBytes)
        {
            if (audioBytes != null && audioBytes.Length > 0)
            {
                return this with { AudioData = Convert.ToBase64String(audioBytes) };
            }
            else
            {
                return this with { AudioData = null };
            }
        }
    }
}
