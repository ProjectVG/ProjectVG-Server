namespace ProjectVG.Application.Models.Chat
{
    public class ChatProcessResult
    {
        public string Response { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public double Cost { get; set; }
        
        /// <summary>
        /// 메시지 세그먼트 리스트 (텍스트와 오디오가 함께 묶여있음)
        /// </summary>
        public List<ChatMessageSegment> Segments { get; set; } = new List<ChatMessageSegment>();
        
        /// <summary>
        /// 전체 응답 텍스트 (모든 세그먼트의 텍스트를 합친 것)
        /// </summary>
        public string FullText => string.Join(" ", Segments.Where(s => s.HasText).Select(s => s.Text));
        
        /// <summary>
        /// 오디오가 있는 세그먼트가 있는지 확인
        /// </summary>
        public bool HasAudio => Segments.Any(s => s.HasAudio);
        
        /// <summary>
        /// 텍스트가 있는 세그먼트가 있는지 확인
        /// </summary>
        public bool HasText => Segments.Any(s => s.HasText);
    }
} 