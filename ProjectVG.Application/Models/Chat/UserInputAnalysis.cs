namespace ProjectVG.Application.Models.Chat
{
    public class UserInputAnalysis
    {
        public string Intent { get; set; } = string.Empty;
        public DateTime? ContextTime { get; set; }
        public List<string> Keywords { get; set; } = new();
        public string Action { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? RejectionReason { get; set; }
        public string? EnhancedQuery { get; set; }

        public static UserInputAnalysis Valid(string intent, DateTime? contextTime, List<string> keywords, string action, string? enhancedQuery = null)
        {
            return new UserInputAnalysis
            {
                Intent = intent,
                ContextTime = contextTime,
                Keywords = keywords,
                Action = action,
                IsValid = true,
                EnhancedQuery = enhancedQuery
            };
        }

        public static UserInputAnalysis Invalid(string rejectionReason, string action)
        {
            return new UserInputAnalysis
            {
                RejectionReason = rejectionReason,
                Action = action,
                IsValid = false
            };
        }
    }
}
