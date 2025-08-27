using Mono.TextTemplating;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.Factories;
using ProjectVG.Infrastructure.Integrations.LLMClient;
using System.Diagnostics;

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
            context.SetResponse(llmResponse.Response, segments, cost);

            _logger.LogInformation("채팅 처리 결과: {Response}\n 세그먼트 생성 개수: {SementCount}\n 입력 토큰: {InputTokens}\n 출력 토큰: {OutputTokens}\n 비용: {Cost}",
                llmResponse.Response, segments.Count, llmResponse.InputTokens, llmResponse.OutputTokens, cost);
        }
    }
}
