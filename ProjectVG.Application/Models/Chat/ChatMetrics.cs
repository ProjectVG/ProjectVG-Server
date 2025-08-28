namespace ProjectVG.Application.Models.Chat
{
    public class ChatMetrics
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string CharacterId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<ProcessMetrics> ProcessMetrics { get; set; } = new();
        public decimal TotalCost { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }

    public class ProcessMetrics
    {
        public string ProcessName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public decimal Cost { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }
}
