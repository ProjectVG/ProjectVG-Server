namespace ProjectVG.Application.Models.Chat
{
    public class UserInputAnalysis
    {
        /// <summary>
        /// 대화 맥락 - 현재 대화 맥락을 분석하여 배경 등 어떤 상황인지 추론
        /// </summary>
        public string? ConversationContext { get; set; }
        
        /// <summary>
        /// 질문의도 - 사용자의 userprompt를 분석한 의도
        /// </summary>
        public string? UserIntent { get; set; }
        
        /// <summary>
        /// AI가 취해야할 액션
        /// </summary>
        public UserInputAction Action { get; set; }
        
        /// <summary>
        /// 실패 이유 (액션이 Reject, Clarify일 경우만 필드 존재)
        /// </summary>
        public string? FailureReason { get; set; }
        
        /// <summary>
        /// 키워드 - 대화 주제, userprompt에서 추출 가능한 키워드 (VectorDB를 위한 필드)
        /// </summary>
        public List<string> Keywords { get; set; } = new List<string>();
        
        /// <summary>
        /// 향상된 검색 쿼리 (VectorDB를 위한 필드)
        /// </summary>
        public string? EnhancedQuery { get; set; }
        
        /// <summary>
        /// 참고할 대화 시간대 (VectorDB를 위한 필드)
        /// </summary>
        public DateTime? ContextTime { get; set; }

        private UserInputAnalysis()
        {
        }

        /// <summary>
        /// 정상적인 대화 분석 결과 생성
        /// </summary>
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

        /// <summary>
        /// 무시할 입력 분석 결과 생성
        /// </summary>
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

        /// <summary>
        /// 거절할 입력 분석 결과 생성
        /// </summary>
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



        /// <summary>
        /// 액션이 Chat인지 확인
        /// </summary>
        public bool IsValid => Action == UserInputAction.Chat;
        
        /// <summary>
        /// 액션이 무시, 거절 중 하나인지 확인 (즉시 종료)
        /// </summary>
        public bool ShouldTerminate => Action == UserInputAction.Ignore || 
                                      Action == UserInputAction.Reject;
    }
}
