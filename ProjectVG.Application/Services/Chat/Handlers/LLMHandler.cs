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

            _logger.LogInformation("LLM 원본 응답: {RawResponse}", llmResponse.Response);

            var parsed = ChatOutputFormat.Parse(llmResponse.Response, context.VoiceName);

            _logger.LogInformation("채팅 응답 결과 - 응답: {Response}, 감정: {Emotion}, 토큰 사용량: {TokensUsed}", 
                parsed.Response, string.Join(",", parsed.Emotion), llmResponse.TokensUsed);

            double cost = 0;
            if (llmResponse.TokensUsed > 0)
            {
                cost = Math.Ceiling(llmResponse.TokensUsed / 25.0);
            }

            // 세그먼트 생성
            var segments = new List<ChatMessageSegment>();
            for (int i = 0; i < parsed.Text.Count; i++)
            {
                var emotion = parsed.Emotion.Count > i ? parsed.Emotion[i] : "neutral";
                var segment = ChatMessageSegment.CreateTextOnly(parsed.Text[i], i);
                segment.Emotion = emotion;
                segments.Add(segment);
            }

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