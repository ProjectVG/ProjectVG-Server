using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.LLM;
using Microsoft.Extensions.Logging;
using ProjectVG.Common.Constants;

namespace ProjectVG.Application.Services.Chat.Handlers
{
    public class LLMHandler
    {
        private readonly ILLMService _llmService;
        private readonly ILogger<LLMHandler> _logger;

        public LLMHandler(ILLMService llmService, ILogger<LLMHandler> logger)
        {
            _llmService = llmService;
            _logger = logger;
        }

        public async Task<ChatProcessResult> ProcessLLMAsync(ChatPreprocessContext context)
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
            var outputFormat = new ChatOutputFormat(context.AllowedEmotions);
            var parsed = outputFormat.Parse(llmResponse.Response);

            return new ChatProcessResult
            {
                Response = parsed.Response,
                Emotion = parsed.Emotion,
                Summary = parsed.Summary,
                TokensUsed = llmResponse.TokensUsed
            };
        }
    }
} 