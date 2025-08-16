namespace ProjectVG.Application.Models.Chat
{
    public class UserInputAnalysis
    {
        public string? ConversationContext { get; set; }
        public string? UserIntent { get; set; }
        public UserInputAction Action { get; set; }
        public string? FailureReason { get; set; }
        public List<string> Keywords { get; set; } = new List<string>();
        public string? EnhancedQuery { get; set; }
        public DateTime? ContextTime { get; set; }

        private UserInputAnalysis()
        {
        }

        public static UserInputAnalysis CreateValid(
            string conversationContext,
            string userIntent,
            UserInputAction action,
            List<string> keywords,
            string? enhancedQuery = null,
            DateTime? contextTime = null)
        {
            return new UserInputAnalysis
            {
                ConversationContext = conversationContext,
                UserIntent = userIntent,
                Action = action,
                Keywords = keywords,
                EnhancedQuery = enhancedQuery,
                ContextTime = contextTime
            };
        }

        public static UserInputAnalysis CreateIgnore(string failureReason)
        {
            return new UserInputAnalysis
            {
                ConversationContext = null,
                UserIntent = null,
                Action = UserInputAction.Ignore,
                FailureReason = failureReason
            };
        }

        public static UserInputAnalysis CreateReject(string failureReason)
        {
            return new UserInputAnalysis
            {
                ConversationContext = null,
                UserIntent = null,
                Action = UserInputAction.Reject,
                FailureReason = failureReason
            };
        }

        public bool IsValid => Action == UserInputAction.Chat;
        
        public bool ShouldTerminate => Action == UserInputAction.Ignore || 
                                      Action == UserInputAction.Reject;
    }
}
