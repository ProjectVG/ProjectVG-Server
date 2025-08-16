namespace ProjectVG.Application.Models.Chat
{
    public class ChatMessageSegment
    {
        public string? Text { get; set; }
        public byte[]? AudioData { get; set; }
        public string? AudioContentType { get; set; }
        public float? AudioLength { get; set; }
        public string? Emotion { get; set; }
        public int Order { get; set; }
        
        public bool HasText => !string.IsNullOrEmpty(Text);
        public bool HasAudio => AudioData != null && AudioData.Length > 0;
        public bool IsEmpty => !HasText && !HasAudio;
        
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
        
        public string ToDebugString()
        {
            return $"Segment[Order={Order}, Text={HasText}, Audio={HasAudio}, Emotion={Emotion ?? "none"}, AudioSize={AudioData?.Length ?? 0} bytes, AudioLength={AudioLength:F2}s]";
        }
        
        public static ChatMessageSegment CreateTextOnly(string text, int order = 0)
        {
            return new ChatMessageSegment
            {
                Text = text,
                Order = order
            };
        }
        
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
