namespace ProjectVG.Application.Models.Chat
{
    /// <summary>
    /// 채팅 메시지의 하나의 세그먼트 (텍스트 + 오디오 쌍)
    /// </summary>
    public class ChatMessageSegment
    {
        /// <summary>
        /// 텍스트 메시지 (선택사항)
        /// </summary>
        public string? Text { get; set; }
        
        /// <summary>
        /// 오디오 데이터 (선택사항)
        /// </summary>
        public byte[]? AudioData { get; set; }
        
        /// <summary>
        /// 오디오 콘텐츠 타입 (예: "audio/wav")
        /// </summary>
        public string? AudioContentType { get; set; }
        
        /// <summary>
        /// 오디오 길이 (초)
        /// </summary>
        public float? AudioLength { get; set; }
        
        /// <summary>
        /// 감정 (예: "happy", "sad", "neutral")
        /// </summary>
        public string? Emotion { get; set; }
        
        /// <summary>
        /// 세그먼트 순서 (0부터 시작)
        /// </summary>
        public int Order { get; set; }
        
        /// <summary>
        /// 텍스트가 있는지 확인
        /// </summary>
        public bool HasText => !string.IsNullOrEmpty(Text);
        
        /// <summary>
        /// 오디오가 있는지 확인
        /// </summary>
        public bool HasAudio => AudioData != null && AudioData.Length > 0;
        
        /// <summary>
        /// 빈 세그먼트인지 확인
        /// </summary>
        public bool IsEmpty => !HasText && !HasAudio;
        
        /// <summary>
        /// 세그먼트 내용을 문자열로 출력
        /// </summary>
        public override string ToString()
        {
            var parts = new List<string>();
            
            parts.Add($"Order: {Order}");
            
            if (HasText)
            {
                parts.Add($"Text: \"{Text}\"");
            }
            
            if (HasAudio)
            {
                parts.Add($"Audio: {AudioData!.Length} bytes, {AudioContentType}, {AudioLength:F2}s");
            }
            
            if (!string.IsNullOrEmpty(Emotion))
            {
                parts.Add($"Emotion: {Emotion}");
            }
            
            return $"Segment({string.Join(", ", parts)})";
        }
        
        /// <summary>
        /// 세그먼트 내용을 간단한 문자열로 출력
        /// </summary>
        public string ToShortString()
        {
            var parts = new List<string>();
            
            if (HasText)
            {
                parts.Add($"\"{Text}\"");
            }
            
            if (HasAudio)
            {
                parts.Add($"[Audio: {AudioLength:F1}s]");
            }
            
            return string.Join(" ", parts);
        }
        
        /// <summary>
        /// 디버그용 상세 정보 출력
        /// </summary>
        public string ToDebugString()
        {
            return $"Segment[Order={Order}, Text={HasText}, Audio={HasAudio}, Emotion={Emotion ?? "none"}, AudioSize={AudioData?.Length ?? 0} bytes, AudioLength={AudioLength:F2}s]";
        }
        
        /// <summary>
        /// 텍스트만 있는 세그먼트 생성
        /// </summary>
        public static ChatMessageSegment CreateTextOnly(string text, int order = 0)
        {
            return new ChatMessageSegment
            {
                Text = text,
                Order = order
            };
        }
        
        /// <summary>
        /// 오디오만 있는 세그먼트 생성
        /// </summary>
        public static ChatMessageSegment CreateAudioOnly(byte[] audioData, string contentType, float? audioLength, int order = 0)
        {
            return new ChatMessageSegment
            {
                AudioData = audioData,
                AudioContentType = contentType,
                AudioLength = audioLength,
                Order = order
            };
        }
        
        /// <summary>
        /// 텍스트와 오디오가 모두 있는 세그먼트 생성
        /// </summary>
        public static ChatMessageSegment CreateIntegrated(string text, byte[] audioData, string contentType, float? audioLength, string? emotion = null, int order = 0)
        {
            return new ChatMessageSegment
            {
                Text = text,
                AudioData = audioData,
                AudioContentType = contentType,
                AudioLength = audioLength,
                Emotion = emotion,
                Order = order
            };
        }
    }
} 