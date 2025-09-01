using System.Text.Json.Serialization;

namespace ProjectVG.Application.Models.Chat
{
    public record ChatProcessResultMessage
    {
        [JsonPropertyName("request_id")]
        public string? RequestId { get; init; }

        [JsonPropertyName("type")]
        public string Type { get; init; } = "chat";
        
        [JsonPropertyName("text")]
        public string? Text { get; init; }

        [JsonPropertyName("emotion")]
        public string? Emotion { get; init; }

        [JsonPropertyName("actions")]
        public string[]? Actions { get; init; }

        [JsonPropertyName("audio_data")]
        public string? AudioData { get; init; }
        
        [JsonPropertyName("audio_format")]
        public string? AudioFormat { get; init; }
        
        [JsonPropertyName("audio_length")]
        public float? AudioLength { get; init; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        
        [JsonPropertyName("order")]
        public int Order { get; init; }
        
        public static ChatProcessResultMessage FromSegment(ChatSegment segment, string? requestId = null)
        {
            var audioData = segment.HasAudio ? Convert.ToBase64String(segment.AudioData!) : null;
            var audioFormat = segment.HasAudio ? segment.AudioContentType ?? "wav" : null;
            
            return new ChatProcessResultMessage
            {
                RequestId = requestId,
                Type = "chat",
                Text = segment.Content,
                Emotion = segment.Emotion,
                Actions = segment.Actions?.ToArray(),
                AudioData = audioData,
                AudioFormat = audioFormat,
                AudioLength = segment.AudioLength,
                Order = segment.Order,
                Timestamp = DateTime.UtcNow
            };
        }
        
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
