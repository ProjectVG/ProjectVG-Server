namespace ProjectVG.Application.Models.Chat
{
    public class ChatOutputFormatResult
    {
        public string Response { get; set; } = string.Empty;
        public List<string> Emotion { get; set; } = new List<string>();
        public List<string> Text { get; set; } = new List<string>();
    }
}
