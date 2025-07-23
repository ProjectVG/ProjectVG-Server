namespace ProjectVG.Application.Models.Chat
{
    public class ChatProcessResult
    {
        public string Response { get; set; } = string.Empty;
        public List<string> Emotion { get; set; } = new List<string>();
        public List<string> Text { get; set; } = new List<string>();
        public int TokensUsed { get; set; }
        public double Cost { get; set; }
        public List<byte[]> AudioDataList { get; set; } = new List<byte[]>();
        public List<string?> AudioContentTypeList { get; set; } = new List<string?>();
        public List<float?> AudioLengthList { get; set; } = new List<float?>();
        // 기존 단일 필드는 하위 호환을 위해 남겨둡니다.
        public byte[]? AudioData { get; set; }
        public string? AudioContentType { get; set; }
        public float? AudioLength { get; set; }
    }
} 