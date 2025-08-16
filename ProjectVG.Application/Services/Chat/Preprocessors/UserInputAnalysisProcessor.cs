using ProjectVG.Application.Services.Chat.Factories;
using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Infrastructure.Integrations.LLMClient;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat.Preprocessors
{
    public class UserInputAnalysisProcessor
    {
        private readonly ILLMClient _llmClient;
        private readonly ILogger<UserInputAnalysisProcessor> _logger;

        /// <summary>
        /// LLM 클라이언트와 로거를 받아 UserInputAnalysisProcessor 인스턴스를 초기화합니다.
        /// </summary>
        public UserInputAnalysisProcessor(
            ILLMClient llmClient,
            ILogger<UserInputAnalysisProcessor> logger)
        {
            _llmClient = llmClient;
            _logger = logger;
        }

        /// <summary>
        /// 주어진 사용자 입력과 최근 대화 맥락을 사용해 LLM을 호출하여 사용자 입력 분석(UserInputAnalysis)을 생성한다.
        /// </summary>
        /// <remarks>
        /// 최근 대화 내역 중 최대 5개를 맥락으로 포함하고, 포맷 객체가 제공하는 시스템 메시지·지시문·모델 설정을 사용해 LLM 응답을 요청한 뒤 이를 파싱하여 분석 결과를 반환한다.
        /// 오류가 발생하면 기본값을 갖는 유효한 UserInputAnalysis를 반환한다.
        /// </remarks>
        /// <param name="userInput">분석할 원문 사용자 입력.</param>
        /// <param name="conversationHistory">최근 대화 기록(역할과 내용이 포함된 항목들). 최대 5개 항목이 맥락으로 사용된다.</param>
        /// <returns>LLM에서 파싱한 UserInputAnalysis(오류 시 기본값을 가진 분석을 반환).</returns>
        public async Task<UserInputAnalysis> ProcessAsync(string userInput, IEnumerable<ConversationHistory> conversationHistory)
        {
            try
            {
                var format = LLMFormatFactory.CreateUserInputAnalysisFormat();
                
                // 최근 5개만 파싱
                var recentContext = conversationHistory
                    .Take(5)
                    .Select(c => $"{c.Role}: {c.Content}")
                    .ToList();
                
                var llmResponse = await _llmClient.CreateTextResponseAsync(
                    format.GetSystemMessage(userInput),
                    userInput,
                    format.GetInstructions(userInput),
                    recentContext,
                    model: format.Model,
                    maxTokens: format.MaxTokens,
                    temperature: format.Temperature
                );

                var analysis = format.Parse(llmResponse.Response, userInput);
                
                _logger.LogDebug("사용자 입력 분석 완료: '{Input}' -> 맥락: {Context}, 의도: {Intent}, 액션: {Action}", 
                    userInput, analysis.ConversationContext, analysis.UserIntent, analysis.Action);

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 입력 분석 중 오류 발생: '{Input}'", userInput);
                // 오류 발생 시 기본값 반환
                return UserInputAnalysis.CreateValid("일반적인 대화", "대화", UserInputAction.Chat, new List<string>());
            }
        }
    }
}
