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

            var parsed = ChatOutputFormat.Parse(llmResponse.Response, context.VoiceName);
            var cost = CalculateCost(llmResponse.TokensUsed);
            var segments = CreateSegments(parsed);

            return new ChatProcessResult
            {
                Response = parsed.Response,
                Segments = segments,
                TokensUsed = llmResponse.TokensUsed,
                Cost = cost
            };
        }

        private static double CalculateCost(int tokensUsed)
        {
            return tokensUsed > 0 ? Math.Ceiling(tokensUsed / 25.0) : 0;
        }

        private static List<ChatMessageSegment> CreateSegments(ChatOutputFormatResult parsed)
        {
            var segments = new List<ChatMessageSegment>();
            for (int i = 0; i < parsed.Text.Count; i++)
            {
                var emotion = parsed.Emotion.Count > i ? parsed.Emotion[i] : "neutral";
                var segment = ChatMessageSegment.CreateTextOnly(parsed.Text[i], i);
                segment.Emotion = emotion;
                segments.Add(segment);
            }
            return segments;
        }
    }
} 