namespace ProjectVG.Application.Models.Chat
{
    public class ChatProcessResult
    {
        public string Response { get; set; } = string.Empty;
        public string Emotion { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
    }
} 