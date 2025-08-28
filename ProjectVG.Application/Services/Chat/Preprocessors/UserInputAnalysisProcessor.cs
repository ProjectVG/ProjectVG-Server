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

        public UserInputAnalysisProcessor(
            ILLMClient llmClient,
            ILogger<UserInputAnalysisProcessor> logger)
        {
            _llmClient = llmClient;
            _logger = logger;
        }

        /// <summary>
        /// 주어진 사용자 입력과 최근 대화 맥락을 바탕으로 LLM에 질의하여 입력의 의도·액션·대화 맥락 등을 분석한 결과를 비동기로 반환합니다.
        /// </summary>
        /// <param name="userInput">분석할 원문 사용자 입력 문자열.</param>
        /// <param name="conversationHistory">최근 대화 맥락으로 사용할 대화 이력 컬렉션(내부에서 최대 5건만 사용).</param>
        /// <returns>분석 결과를 담은 <see cref="UserInputAnalysis"/>를 반환하는 <see cref="Task"/>. 오류 발생 시 기본 유효 분석 객체를 반환합니다.</returns>
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

                var cost = format.CalculateCost(llmResponse.InputTokens, llmResponse.OutputTokens);
                var analysis = format.Parse(llmResponse.Response, userInput);
                analysis.Cost = cost;
                
                Console.WriteLine($"[USER_INPUT_ANALYSIS_DEBUG] ID: {llmResponse.Id}, 입력 토큰: {llmResponse.InputTokens}, 출력 토큰: {llmResponse.OutputTokens}, 총 토큰: {llmResponse.TokensUsed}, 계산된 비용: {cost:F0} Cost");
                _logger.LogDebug("사용자 입력 분석 완료: '{Input}' -> 맥락: {Context}, 의도: {Intent}, 액션: {Action}, 비용: {Cost}", 
                    userInput, analysis.ConversationContext, analysis.UserIntent, analysis.Action, cost);

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
