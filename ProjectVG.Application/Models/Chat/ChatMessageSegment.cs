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
        
        
        public static ChatMessageSegment CreateTextOnly(string text, int order = 0)
        {
            return new ChatMessageSegment
            {
                Text = text,
                Order = order
            };
        }
        

    }
}
