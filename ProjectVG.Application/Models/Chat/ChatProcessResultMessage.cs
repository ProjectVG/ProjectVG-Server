using System.Text.Json.Serialization;
using System.Buffers;
using System.Buffers.Text;

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
        
        [JsonPropertyName("credits_used")]
        public decimal? CreditsUsed { get; init; }
        
        [JsonPropertyName("credits_remaining")]
        public decimal? CreditsRemaining { get; init; }
        
        public static ChatProcessResultMessage FromSegment(ChatSegment segment, string? requestId = null)
        {
            var audioData = segment.HasAudio ? ConvertToBase64Optimized(segment.GetAudioSpan()) : null;
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
                return this with { AudioData = ConvertToBase64Optimized(new ReadOnlySpan<byte>(audioBytes)) };
            }
            else
            {
                return this with { AudioData = null };
            }
        }

        /// <summary>
        /// ArrayPool을 사용한 메모리 효율적인 Base64 인코딩 (LOH 방지)
        /// </summary>
        private static string? ConvertToBase64Optimized(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty) return null;

            var arrayPool = ArrayPool<byte>.Shared;
            var base64Length = Base64.GetMaxEncodedToUtf8Length(data.Length);
            var buffer = arrayPool.Rent(base64Length);

            try
            {
                if (Base64.EncodeToUtf8(data, buffer, out _, out var bytesWritten) == OperationStatus.Done)
                {
                    // UTF8 바이트를 문자열로 변환
                    return System.Text.Encoding.UTF8.GetString(buffer, 0, bytesWritten);
                }
                else
                {
                    // 폴백: 기존 방법 사용
                    return Convert.ToBase64String(data);
                }
            }
            finally
            {
                arrayPool.Return(buffer);
            }
        }
        
        public ChatProcessResultMessage WithCreditInfo(decimal? creditsUsed, decimal? creditsRemaining)
        {
            return this with { CreditsUsed = creditsUsed, CreditsRemaining = creditsRemaining };
        }
    }
}
