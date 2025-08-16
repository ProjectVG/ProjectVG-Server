using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.Factories;
using ProjectVG.Infrastructure.Integrations.LLMClient;

namespace ProjectVG.Application.Services.Chat.Processors
{
    public class ChatLLMProcessor
    {
        private readonly ILLMClient _llmClient;
        private readonly ILogger<ChatLLMProcessor> _logger;

        /// <summary>
        /// ChatLLMProcessor의 인스턴스를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// LLM 클라이언트와 로거를 주입 받아 내부 필드에 할당합니다.
        /// </remarks>
        public ChatLLMProcessor(
            ILLMClient llmClient,
            ILogger<ChatLLMProcessor> logger)
        {
            _llmClient = llmClient;
            _logger = logger;
        }

        /// <summary>
        /// LLM에 사용자 및 대화 문맥을 전달하여 응답을 생성하고 파싱한 뒤, 세그먼트와 비용 정보를 포함한 처리 결과를 반환합니다.
        /// </summary>
        /// <param name="context">처리에 필요한 입력 컨텍스트(사용자 메시지, 세션 ID, 메모리 컨텍스트, 대화 히스토리 파싱 메서드 등을 포함).</param>
        /// <returns>파싱된 최종 응답 텍스트, 생성된 세그먼트 목록, 사용된 토큰 수 및 계산된 비용을 포함한 <see cref="ChatProcessResult"/>.</returns>
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
            var cost = format.CalculateCost(llmResponse.TokensUsed);
            var segments = CreateSegments(parsed);

            _logger.LogDebug("LLM 처리 완료: 세션 {SessionId}, 토큰 {TokensUsed}, 비용 {Cost}",
                context.SessionId, llmResponse.TokensUsed, cost);

            return new ChatProcessResult {
                Response = parsed.Response,
                Segments = segments,
                TokensUsed = llmResponse.TokensUsed,
                Cost = cost
            };
        }

        /// <summary>
        /// 파싱된 출력 결과(ChatOutputFormatResult)를 ChatMessageSegment 목록으로 변환합니다.
        /// </summary>
        /// <param name="parsed">LLM 포맷 파싱 결과; 텍스트 조각(parsed.Text)과 감정 리스트(parsed.Emotion)를 포함해야 합니다.</param>
        /// <returns>parsed.Text의 각 항목에 대응하는 ChatMessageSegment 목록(각 항목의 인덱스가 세그먼트 순서를 결정).</returns>
        /// <remarks>
        /// 각 세그먼트의 Emotion은 parsed.Emotion에 동일 인덱스 값이 있으면 그 값을 사용하고, 없으면 "neutral"으로 설정됩니다.
        /// </remarks>
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
