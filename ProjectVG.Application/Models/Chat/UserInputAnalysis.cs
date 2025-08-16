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

        /// <summary>
        /// 인스턴스의 직접 생성을 금지하는 비공개 생성자입니다.
        /// </summary>
        /// <remarks>
        /// 이 클래스는 정적 팩토리 메서드(CreateValid, CreateIgnore, CreateReject)를 통해서만 생성되어야 합니다.
        /// </remarks>
        private UserInputAnalysis()
        {
        }

        /// <summary>
        /// 사용자 입력 분석 결과의 유효한 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="conversationContext">대화의 문맥(관련된 이전 메시지나 컨텍스트 문자열).</param>
        /// <param name="userIntent">추출된 사용자의 의도(의도 태그 또는 설명).</param>
        /// <param name="action">분석 결과에 따른 동작을 나타내는 값(예: Chat, Ignore, Reject 등).</param>
        /// <param name="keywords">입력에서 추출된 키워드 목록.</param>
        /// <param name="enhancedQuery">(선택) 증강된 쿼리나 보강된 사용자 입력 문자열.</param>
        /// <param name="contextTime">(선택) 문맥이 유효한 시간 또는 캡처 시각.</param>
        /// <returns>주어진 값들로 초기화된 UserInputAnalysis의 새 인스턴스.</returns>
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
        /// 사용자 입력 분석 결과를 '무시(Ignore)' 상태로 생성합니다.
        /// </summary>
        /// <param name="failureReason">무시된 이유를 설명하는 메시지(저장됨).</param>
        /// <returns>
        /// ConversationContext와 UserIntent가 null이고 Action이 <c>UserInputAction.Ignore</c>이며 FailureReason이 설정된 <see cref="UserInputAnalysis"/> 인스턴스.
        /// </returns>
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
        /// 거부된 사용자 입력을 나타내는 UserInputAnalysis 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="failureReason">거부 사유을 설명하는 메시지.</param>
        /// <returns>Action이 <c>UserInputAction.Reject</c>로 설정되고 대화 문맥과 의도가 비워진 새 <see cref="UserInputAnalysis"/> 인스턴스.</returns>
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
