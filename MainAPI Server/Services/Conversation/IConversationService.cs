using MainAPI_Server.Models.Chat;

namespace MainAPI_Server.Services.Conversation
{
    public interface IConversationService
    {

        /// <summary>
        /// 세션 대화 기록 추가
        /// </summary>
        /// <param name="SesstionId">세션 ID</param>
        /// <param name="role">주체</param>
        /// <param name="content">대화 내용</param>
        void AddMessage(string SesstionId, MessageRole role, string content);

        /// <summary>
        /// 세션 대화 기록 추가
        /// </summary>
        /// <param name="message">대화 기록</param>
        void AddMessage(ChatMessage message);

        /// <summary>
        /// 세션 대화 기록 조회
        /// </summary>
        /// <param name="sessionId">세션ID</param>
        /// <param name="count">세션 대화 기록 개수</param>
        /// <returns>대화 기록 목록</returns>
        List<ChatMessage> GetConversationHistory(string sessionId, int count = 5);

        /// <summary>
        /// 세션 대화 기록 삭제
        /// </summary>
        /// <param name="sessionId">세션ID</param>
        void ClearConversation(string sessionId);
    }
} 