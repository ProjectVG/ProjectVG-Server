namespace ProjectVG.Application.Models.Chat
{
    public class UserInputAnalysis
    {

        // 필요한 필드
        // 1. 예측한 user prompt의 의도
        public string UserIntent { get; set; } = string.Empty;
        
        // 2. 요청 처리 Type (기존 UserInputAction의 새로운 명칭)
        public UserInputProcessType ProcessType { get; set; }
        
        // 3. 실패 이유 (ProcessType이 Ignore나 Reject일 때만 사용)
        public string? FailureReason { get; set; }
        
        // 비용 추적용 (기존 유지)
        public double Cost { get; set; }

        private UserInputAnalysis()
        {
        }

        public static UserInputAnalysis CreateValid(
            string userIntent,
            UserInputProcessType processType = UserInputProcessType.Chat,
            double cost = 0)
        {
            return new UserInputAnalysis
            {
                UserIntent = userIntent,
                ProcessType = processType,
                Cost = cost
            };
        }

        public static UserInputAnalysis CreateIgnore(string failureReason)
        {
            return new UserInputAnalysis
            {
                UserIntent = string.Empty,
                ProcessType = UserInputProcessType.Ignore,
                FailureReason = failureReason
            };
        }

        public static UserInputAnalysis CreateReject(string failureReason)
        {
            return new UserInputAnalysis
            {
                UserIntent = string.Empty,
                ProcessType = UserInputProcessType.Reject,
                FailureReason = failureReason
            };
        }

        public bool IsValid => ProcessType == UserInputProcessType.Chat;
        
        public bool ShouldTerminate => ProcessType == UserInputProcessType.Ignore || 
                                      ProcessType == UserInputProcessType.Reject;
    }
}
