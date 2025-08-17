using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.Factories;
using ProjectVG.Infrastructure.Integrations.LLMClient;

namespace ProjectVG.Application.Services.Chat.Processors
{
    public class ChatLLMProcessor
    {
        private readonly ILLMClient _llmClient;
        private readonly ILogger<ChatLLMProcessor> _logger;

        public ChatLLMProcessor(
            ILLMClient llmClient,
            ILogger<ChatLLMProcessor> logger)
        {
            _llmClient = llmClient;
            _logger = logger;
        }

        public async Task ProcessAsync(ChatProcessContext context)
        {
            var format = LLMFormatFactory.CreateChatFormat();

            var llmResponse = await _llmClient.CreateTextResponseAsync(
                    format.GetSystemMessage(context),
                    context.UserMessage,
                    format.GetInstructions(context),
                    context.ParseConversationHistory().ToList(),
                    context.MemoryContext?.ToList(),
                    model: format.Model,
                    maxTokens: format.MaxTokens,
                    temperature: format.Temperature
                );

            var segments = format.Parse(llmResponse.Response, context);
            var cost = format.CalculateCost(llmResponse.InputTokens, llmResponse.OutputTokens);

            Console.WriteLine($"[LLM_DEBUG] ID: {llmResponse.Id}, 입력 토큰: {llmResponse.InputTokens}, 출력 토큰: {llmResponse.OutputTokens}, 계산된 비용: {cost:F0} Cost");
            _logger.LogDebug("LLM 처리 완료: 세션 {SessionId}, ID {Id}, 입력 토큰 {InputTokens}, 출력 토큰 {OutputTokens}, 비용 {Cost}",
                context.SessionId, llmResponse.Id, llmResponse.InputTokens, llmResponse.OutputTokens, cost);

            context.SetResponse(llmResponse.Response, segments, cost);
        }
    }
}
