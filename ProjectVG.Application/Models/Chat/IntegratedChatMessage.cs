using System.Text.Json.Serialization;

namespace ProjectVG.Application.Models.Chat
{
    public class IntegratedChatMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "chat";
        
        [JsonPropertyName("message_type")]
        public string MessageType { get; set; } = "json";
        
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;
        
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        
        [JsonPropertyName("audio_data")]
        public string? AudioData { get; set; }
        
        [JsonPropertyName("audio_format")]
        public string? AudioFormat { get; set; } = "wav";
        
        [JsonPropertyName("audio_length")]
        public float? AudioLength { get; set; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
        
        public void SetAudioData(byte[]? audioBytes)
        {
            if (audioBytes != null && audioBytes.Length > 0)
            {
                AudioData = Convert.ToBase64String(audioBytes);
            }
            else
            {
                AudioData = null;
            }
        }
    }
}
