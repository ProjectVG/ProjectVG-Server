using ProjectVG.Application.Models.Chat;
using ProjectVG.Common.Constants;
using ProjectVG.Application.Services.Chat.Factories;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.LLMClient.Models;
using ProjectVG.Infrastructure.Integrations.LLMClient;

namespace ProjectVG.Application.Services.Chat
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

        public async Task<ChatProcessResult> ProcessAsync(ChatPreprocessContext context)
        {
            // LLM 포맷 생성
            var format = LLMFormatFactory.CreateChatFormat();
            
            // LLM 요청
            var llmResponse = await _llmClient.CreateTextResponseAsync(
                    format.GetSystemMessage(context),
                    context.UserMessage,
                    format.GetInstructions(context),
                    context.ConversationHistory,
                    context.MemoryContext,
                    model: format.Model,
                    maxTokens: format.MaxTokens,
                    temperature: format.Temperature
                );

            // 결과 파싱
            var parsed = format.Parse(llmResponse.Response, context);
            var cost = format.CalculateCost(llmResponse.TokensUsed);
            var segments = CreateSegments(parsed);

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

        private List<ChatMessageSegment> CreateSegments(ChatOutputFormatResult parsed)
        {
            var segments = new List<ChatMessageSegment>();
            for (int i = 0; i < parsed.Text.Count; i++) {
                var emotion = parsed.Emotion.Count > i ? parsed.Emotion[i] : "neutral";
                var segment = ChatMessageSegment.CreateTextOnly(parsed.Text[i], i);
                segment.Emotion = emotion;
                segments.Add(segment);
            }
            return segments;
        }
    }
}
