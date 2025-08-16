using ProjectVG.Infrastructure.Integrations.LLMClient.Models;

namespace ProjectVG.Infrastructure.Integrations.LLMClient
{
    public interface ILLMClient
    {
        /// <summary>
        /// LLMClient 서버에 요청을 전송하고 응답을 받습니다.
        /// </summary>
        /// <param name="request">LLMClient 요청 객체</param>
        /// <summary>
/// 지정한 LLM 요청을 비동기적으로 전송하고 응답을 반환합니다.
/// </summary>
/// <param name="request">전송할 LLM 요청 페이로드(시스템/사용자 메시지, 옵션 등).</param>
/// <returns>LLM 서버에서 반환한 응답을 포함한 <see cref="LLMResponse"/>의 비동기 작업.</returns>
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
        /// <summary>
            /// 주어진 메시지와 옵션을 바탕으로 텍스트 기반 LLM 요청을 생성하고 비동기적으로 응답을 반환합니다.
            /// </summary>
            /// <param name="systemMessage">시스템 역할 메시지(시스템 지침/맥락).</param>
            /// <param name="userMessage">사용자 입력 메시지(현재 요청의 주 내용).</param>
            /// <param name="instructions">추가 지침(모델에게 부가적으로 전달할 행동/출력 지침, 기본 빈 문자열).</param>
            /// <param name="conversationHistory">이전 대화 기록(문맥 유지용, 선택).</param>
            /// <param name="memoryContext">장기 메모리 또는 관련 컨텍스트(선택).</param>
            /// <param name="model">사용할 모델 이름(기본값: "gpt-4o-mini").</param>
            /// <param name="maxTokens">응답에서 허용할 최대 토큰 수(기본값: 1000).</param>
            /// <param name="temperature">출력 샘플링 온도(0.0-1.0 범위 권장, 기본값: 0.7).</param>
            /// <returns>LLM 클라이언트로부터 받은 LLMResponse를 비동기적으로 반환합니다.</returns>
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