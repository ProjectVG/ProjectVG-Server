namespace ProjectVG.Application.Services.Chat.Core
{
    public class StepMetrics
    {
        public required string StepName { get; set; }
        public double TimeMs { get; set; }
        public double Cost { get; set; }
    }
} 