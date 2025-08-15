using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.LLM;
using ProjectVG.Common.Constants;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatLLMProcessor
    {
        private readonly ILLMService _llmService;
        private readonly ILogger<ChatLLMProcessor> _logger;

        public ChatLLMProcessor(
            ILLMService llmService,
            ILogger<ChatLLMProcessor> logger)
        {
            _llmService = llmService;
            _logger = logger;
        }

        public async Task<ChatProcessResult> ProcessAsync(ChatPreprocessContext context)
        {
            var llmResponse = await _llmService.CreateTextResponseAsync(
                context.SystemMessage,
                context.UserMessage,
                context.Instructions,
                context.MemoryContext,
                context.ConversationHistory,
                LLMSettings.Chat.MaxTokens,
                LLMSettings.Chat.Temperature,
                LLMSettings.Chat.Model
            );

            var parsed = ChatOutputFormat.Parse(llmResponse.Response, context.VoiceName);
            var cost = ChatResultProcessor.CalculateCost(llmResponse.TokensUsed);
            var segments = ChatResultProcessor.CreateSegments(parsed);

            _logger.LogDebug("LLM 처리 완료: 세션 {SessionId}, 토큰 {TokensUsed}, 비용 {Cost}",
                context.SessionId, llmResponse.TokensUsed, cost);

            return new ChatProcessResult
            {
                Response = parsed.Response,
                Segments = segments,
                TokensUsed = llmResponse.TokensUsed,
                Cost = cost
            };
        }
    }
}
