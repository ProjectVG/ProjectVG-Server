using System;

namespace ProjectVG.Application.Models.Chat
{
    public class ChatProcessMetricsDto
    {
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string CharacterId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string LLMResponse { get; set; } = string.Empty;
        public bool TTSEnabled { get; set; }
        public double? TTSAudioLength { get; set; }
        public int LLMTokenUsed { get; set; }
        public double LLMCost { get; set; }
        public double TTSCost { get; set; }
        public double TotalCost { get; set; }
        public double PreprocessTimeMs { get; set; }
        public double LLMTimeMs { get; set; }
        public double TTSTimeMs { get; set; }
        public double PersistTimeMs { get; set; }
        public double SendTimeMs { get; set; }
        public double TotalTimeMs { get; set; }

    }
} 