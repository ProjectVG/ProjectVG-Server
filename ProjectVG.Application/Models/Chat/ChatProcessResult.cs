namespace ProjectVG.Application.Models.Chat
{
    public class ChatProcessResult
    {
        public string Response { get; set; } = string.Empty;
        public double Cost { get; set; }
        
        public List<ChatMessageSegment> Segments { get; set; } = new List<ChatMessageSegment>();
        
        public string FullText => string.Join(" ", Segments.Where(s => s.HasText).Select(s => s.Text));
        
        public bool HasAudio => Segments.Any(s => s.HasAudio);
        
        public bool HasText => Segments.Any(s => s.HasText);
    }
}
