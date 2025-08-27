namespace ProjectVG.Application.Models.Chat
{
    public enum SegmentType
    {
        Text = 0,
        Action = 1
    }

    public class ChatMessageSegment
    {
        public string? Text { get; set; }
        public byte[]? AudioData { get; set; }
        public string? AudioContentType { get; set; }
        public float? AudioLength { get; set; }
        public string? Emotion { get; set; }
        public string? Action { get; set; }
        public SegmentType Type { get; set; } = SegmentType.Text;
        public int Order { get; set; }
        
        public bool HasText => !string.IsNullOrEmpty(Text);
        public bool HasAudio => AudioData != null && AudioData.Length > 0;
        public bool IsEmpty => !HasText && !HasAudio && string.IsNullOrEmpty(Action);
        public bool IsTextSegment => Type == SegmentType.Text && HasText;
        public bool IsActionSegment => Type == SegmentType.Action && !string.IsNullOrEmpty(Action);
        
        
        public static ChatMessageSegment CreateTextOnly(string text, int order = 0)
        {
            return new ChatMessageSegment
            {
                Text = text,
                Type = SegmentType.Text,
                Order = order
            };
        }

        public static ChatMessageSegment CreateActionOnly(string action, int order = 0)
        {
            return new ChatMessageSegment
            {
                Action = action,
                Type = SegmentType.Action,
                Order = order
            };
        }

        public void SetAudioData(byte[]? audioData, string? audioContentType, float? audioLength)
        {
            AudioData = audioData;
            AudioContentType = audioContentType;
            AudioLength = audioLength;
        }
        

    }
}
