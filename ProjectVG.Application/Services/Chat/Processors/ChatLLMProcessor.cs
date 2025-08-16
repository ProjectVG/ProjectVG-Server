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

        public async Task<ChatProcessResult> ProcessAsync(ChatPreprocessContext context)
        {
            var format = LLMFormatFactory.CreateChatFormat();

            var llmResponse = await _llmClient.CreateTextResponseAsync(
                    format.GetSystemMessage(context),
                    context.UserMessage,
                    format.GetInstructions(context),
                    context.ParseConversationHistory(),
                    context.MemoryContext,
                    model: format.Model,
                    maxTokens: format.MaxTokens,
                    temperature: format.Temperature
                );

            // 결과 파싱
            var parsed = format.Parse(llmResponse.Response, context);
            var cost = format.CalculateCost(llmResponse.InputTokens, llmResponse.OutputTokens);
            var segments = CreateSegments(parsed);

            Console.WriteLine($"[LLM_DEBUG] ID: {llmResponse.Id}, 입력 토큰: {llmResponse.InputTokens}, 출력 토큰: {llmResponse.OutputTokens}, 총 토큰: {llmResponse.TokensUsed}, 계산된 비용: {cost:F0} Cost");
            _logger.LogDebug("LLM 처리 완료: 세션 {SessionId}, ID {Id}, 입력 토큰 {InputTokens}, 출력 토큰 {OutputTokens}, 총 토큰 {TotalTokens}, 비용 {Cost}",
                context.SessionId, llmResponse.Id, llmResponse.InputTokens, llmResponse.OutputTokens, llmResponse.TokensUsed, cost);

            return new ChatProcessResult {
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
