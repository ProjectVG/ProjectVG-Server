namespace ProjectVG.Application.Models.Chat
{
    public enum SegmentType
    {
        Text = 0,
        Action = 1
    }

    public record ChatSegment
    {
        public SegmentType Type { get; init; } = SegmentType.Text;
        
        public string Content { get; init; } = string.Empty;
        
        public int Order { get; init; }
        
        public string? Emotion { get; init; }
        
        public byte[]? AudioData { get; init; }
        public string? AudioContentType { get; init; }
        public float? AudioLength { get; init; }



        public bool HasContent => !string.IsNullOrEmpty(Content);
        public bool HasAudio => AudioData != null && AudioData.Length > 0;
        public bool IsEmpty => !HasContent;
        public bool IsTextSegment => Type == SegmentType.Text && HasContent;
        public bool IsActionSegment => Type == SegmentType.Action && HasContent;
        public bool HasEmotion => !string.IsNullOrEmpty(Emotion);
        


        public static ChatSegment CreateText(string content, string? emotion = null, int order = 0)
        {
            return new ChatSegment
            {
                Type = SegmentType.Text,
                Content = content,
                Emotion = emotion,
                Order = order
            };
        }

        public static ChatSegment CreateAction(string action, int order = 0)
        {
            return new ChatSegment
            {
                Type = SegmentType.Action,
                Content = action,
                Order = order
            };
        }

        // Method to add audio data (returns new record instance)
        public ChatSegment WithAudioData(byte[] audioData, string audioContentType, float audioLength)
        {
            return this with 
            { 
                AudioData = audioData, 
                AudioContentType = audioContentType, 
                AudioLength = audioLength 
            };
        }
    }
}
