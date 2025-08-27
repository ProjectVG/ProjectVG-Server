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

        public async Task ProcessAsync(ChatRequestCommand request)
        {
            var format = LLMFormatFactory.CreateUserInputAnalysisFormat();
            var userInput = request.UserPrompt;
            var history = request.ConversationHistory?.Take(5).Select(c => $"{c.Role}: {c.Content}").ToList();

            try {
                var llmResponse = await _llmClient.CreateTextResponseAsync(
                    format.GetSystemMessage(userInput),
                    userInput,
                    format.GetInstructions(userInput),
                    history,
                    model: format.Model,
                    maxTokens: format.MaxTokens,
                    temperature: format.Temperature
                );

                var cost = format.CalculateCost(llmResponse.InputTokens, llmResponse.OutputTokens);
                var (processType, intent) = format.Parse(llmResponse.Response, userInput);

                request.AddCost(cost);
                request.SetAnalysisResult(processType, intent);

                Console.WriteLine($"[USER_INPUT_ANALYSIS_DEBUG] ID: {llmResponse.Id}, 입력 토큰: {llmResponse.InputTokens}, 출력 토큰: {llmResponse.OutputTokens}, 총 토큰: {llmResponse.TokensUsed}, 계산된 비용: {cost:F0} Cost");
                _logger.LogDebug("사용자 입력 분석 완료: '{Input}' -> 의도: {Intent}, 처리타입: {ProcessType}, 비용: {Cost}",
                    userInput, intent, processType, cost);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "사용자 입력 분석 중 오류 발생: '{Input}'", userInput);
            }
        }
    }
}
