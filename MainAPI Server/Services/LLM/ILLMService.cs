using MainAPI_Server.Models.External.LLM;

namespace MainAPI_Server.Services.LLM
{
    public interface ILLMService
    {
        /// <summary>
        /// 텍스트 응답 생성
        /// </summary>
        /// <param name="systemMessage">시스템 메시지</param>
        /// <param name="userMessage">사용자 메시지</param>
        /// <param name="conversationContext">대화 컨텍스트</param>
        /// <param name="memoryContext">메모리 컨텍스트</param>
        /// <returns>LLM 응답</returns>
        Task<LLMResponse> CreateTextResponseAsync(string systemMessage, string userMessage, List<string> conversationContext, List<string> memoryContext);
    }
} 