using MainAPI_Server.Models.Chat;

namespace MainAPI_Server.Services.Conversation
{
    public interface IConversationService
    {
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