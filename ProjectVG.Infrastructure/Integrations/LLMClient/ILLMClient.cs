using ProjectVG.Infrastructure.Integrations.LLMClient.Models;

namespace ProjectVG.Infrastructure.Integrations.LLMClient
{
    public interface ILLMClient
    {
        /// <summary>
        /// LLMClient 서버에 요청을 전송하고 응답을 받습니다.
        /// </summary>
        /// <param name="request">LLMClient 요청 객체</param>
        /// <returns>LLMClient 응답</returns>
        Task<LLMResponse> SendRequestAsync(LLMRequest request);

        /// <summary>
        /// 모든 매개변수를 받아서 LLM 요청을 전송합니다.
        /// </summary>
        /// <param name="systemMessage">시스템 메시지</param>
        /// <param name="userMessage">사용자 메시지</param>
        /// <param name="instructions">지시사항</param>
        /// <param name="conversationHistory">대화 기록</param>
        /// <param name="memoryContext">메모리 컨텍스트</param>
        /// <param name="model">모델명</param>
        /// <param name="maxTokens">최대 토큰 수</param>
        /// <param name="temperature">온도</param>
        /// <returns>LLMClient 응답</returns>
        Task<LLMResponse> CreateTextResponseAsync(
            string systemMessage,
            string userMessage,
            string? instructions = "",
            List<string>? conversationHistory = default,
            List<string>? memoryContext = default,
            string? model = "gpt-4o-mini",
            int? maxTokens = 1000,
            float? temperature = 0.7f);
    }
} 