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
        public double Cost { get; set; }
        public string ContainsTemporalExpression { get; set; } = string.Empty;
        public List<string> Emotions { get; set; } = new List<string>();

        /// <summary>
        /// 외부에서 직접 인스턴스화하는 것을 막기 위한 비공개 기본 생성자입니다.
        /// </summary>
        /// <remarks>
        /// 인스턴스 생성은 CreateValid, CreateIgnore, CreateReject 같은 정적 팩토리 메서드를 통해 이루어져야 합니다.
        /// </remarks>
        private UserInputAnalysis()
        {
        }

        /// <summary>
        /// 사용자 입력 분석 결과의 유효한 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="conversationContext">분석된 대화 문맥(예: 이전 대화 요약).</param>
        /// <param name="userIntent">식별된 사용자 의도(예: 요청의 목적).</param>
        /// <param name="action">분석에 따라 수행할 동작(예: 채팅, 무시, 거부 등).</param>
        /// <param name="keywords">추출된 키워드 목록.</param>
        /// <param name="enhancedQuery">선택적 향상된 질의 문자열(있을 경우).</param>
        /// <param name="contextTime">선택적 문맥 관련 타임스탬프.</param>
        /// <param name="cost">분석과 관련된 비용 값(기본값 0).</param>
        /// <returns>주어진 값들로 초기화된 유효한 <see cref="UserInputAnalysis"/> 인스턴스.</returns>
        public static UserInputAnalysis CreateValid(
            string conversationContext,
            string userIntent,
            UserInputAction action,
            List<string> keywords,
            string? enhancedQuery = null,
            DateTime? contextTime = null,
            double cost = 0)
        {
            return new UserInputAnalysis
            {
                ConversationContext = conversationContext,
                UserIntent = userIntent,
                Action = action,
                Keywords = keywords,
                EnhancedQuery = enhancedQuery,
                ContextTime = contextTime,
                Cost = cost
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
