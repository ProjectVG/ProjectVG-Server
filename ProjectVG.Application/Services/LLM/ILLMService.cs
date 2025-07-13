using ProjectVG.Infrastructure.ExternalApis.LLM.Models;

namespace ProjectVG.Application.Services.LLM
{
    public interface ILLMService
    {
        /// <summary>
        /// 텍스트 응답 생성
        /// </summary>
        /// <param name="systemMessage">시스템 메시지</param>
        /// <param name="userMessage">사용자 메시지</param>
        /// <param name="instructions">지시사항</param>
        /// <param name="memoryContext">메모리 컨텍스트</param>
        /// <param name="conversationHistory">대화 기록</param>
        /// <param name="maxTokens">최대 토큰 수</param>
        /// <param name="temperature">온도</param>
        /// <param name="model">모델명</param>
        /// <returns>LLM 응답</returns>
        Task<LLMResponse> CreateTextResponseAsync(
            string systemMessage,
            string userMessage,
            string instructions,
            List<string> memoryContext,
            List<string> conversationHistory,
            int maxTokens,
            float temperature,
            string model);
    }
} 