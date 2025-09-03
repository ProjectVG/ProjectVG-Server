namespace ProjectVG.Domain.Entities.ConversationHistorys
{
    /// <summary>
    /// 채팅 역할 (소문자 문자열로 저장)
    /// </summary>
    public static class ChatRole
    {
        /// <summary>
        /// 사용자
        /// </summary>
        public const string User = "user";
        
        /// <summary>
        /// AI 어시스턴트
        /// </summary>
        public const string Assistant = "assistant";
        
        /// <summary>
        /// 시스템
        /// </summary>
        public const string System = "system";
        
        /// <summary>
        /// 유효한 역할인지 검증
        /// </summary>
        public static bool IsValid(string role)
        {
            return role == User || role == Assistant || role == System;
        }
        
        /// <summary>
        /// 모든 유효한 역할 반환
        /// </summary>
        public static IEnumerable<string> GetAll()
        {
            yield return User;
            yield return Assistant;
            yield return System;
        }
    }
}
