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
        public string? AudioData { get; set; } // Base64 인코딩된 오디오 데이터
        
        [JsonPropertyName("audio_format")]
        public string? AudioFormat { get; set; } = "wav";
        
        [JsonPropertyName("audio_length")]
        public float? AudioLength { get; set; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
        
        // 오디오 데이터를 Base64로 변환하는 메서드
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