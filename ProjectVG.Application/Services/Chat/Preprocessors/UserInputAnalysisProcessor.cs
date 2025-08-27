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
            var systemPrompt = format.GetSystemMessage(null);
            var Instructions = format.GetInstructions(null);
            var userPrompt = request.UserPrompt;

            try {
                var llmResponse = await _llmClient.CreateTextResponseAsync(
                    systemPrompt,
                    userPrompt,
                    Instructions,
                    null,
                    model: format.Model,
                    maxTokens: format.MaxTokens,
                    temperature: format.Temperature
                );

                var cost = format.CalculateCost(llmResponse.InputTokens, llmResponse.OutputTokens);
                var (processType, intent) = format.Parse(llmResponse.Response, userPrompt);

                request.AddCost(cost);
                request.SetAnalysisResult(processType, intent);

                _logger.LogDebug("사용자 입력 분석 완료: '{Input}' -> 의도: {Intent}, 처리타입: {ProcessType}, 비용: {Cost}",
                    userPrompt, intent, processType, cost);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "사용자 입력 분석 중 오류 발생: '{Input}'", userPrompt);
            }
        }
    }
}
