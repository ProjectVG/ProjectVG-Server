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

        /// <summary>
        /// ChatLLMProcessor의 새 인스턴스를 생성하고 필요한 의존성(LLM 클라이언트 및 로거)을 주입합니다.
        /// </summary>
        public ChatLLMProcessor(
            ILLMClient llmClient,
            ILogger<ChatLLMProcessor> logger)
        {
            _llmClient = llmClient;
            _logger = logger;
        }

        /// <summary>
        /// 지정된 처리 컨텍스트를 사용해 LLM에 질의를 보내고, 응답을 파싱·비용을 계산하여 컨텍스트에 결과를 저장합니다.
        /// </summary>
        /// <remarks>
        /// 이 메서드는 LLM 요청을 비동기적으로 실행하고, 반환된 텍스트 응답을 포맷에 따라 분할(segments)하며
        /// 입력/출력 토큰 수를 기반으로 비용을 계산한 후 컨텍스트에 원본 응답, 세그먼트 및 비용을 설정합니다.
        /// 또한 내부 로깅을 통해 처리 세부 정보를 남깁니다.
        /// </remarks>
        /// <param name="context">처리에 필요한 대화 상태, 사용자 메시지, 세션 식별자 등을 포함하는 컨텍스트. 호출 후 응답·세그먼트·비용이 이 컨텍스트에 저장됩니다.</param>
        public async Task ProcessAsync(ChatProcessContext context)
        {
            var format = LLMFormatFactory.CreateChatFormat();

            var llmResponse = await _llmClient.CreateTextResponseAsync(
                    format.GetSystemMessage(context),
                    context.UserMessage,
                    format.GetInstructions(context),
                    context.ParseConversationHistory().ToList(),
                    model: format.Model,
                    maxTokens: format.MaxTokens,
                    temperature: format.Temperature
                );

            var segments = format.Parse(llmResponse.OutputText, context);
            var cost = format.CalculateCost(llmResponse.InputTokens, llmResponse.OutputTokens);
            context.SetResponse(llmResponse.OutputText, segments, cost);

            _logger.LogInformation("채팅 처리 결과: {Response}\n 세그먼트 생성 개수: {SementCount}\n 입력 토큰: {InputTokens}\n 출력 토큰: {OutputTokens}\n 비용: {Cost}",
                llmResponse.OutputText, segments.Count, llmResponse.InputTokens, llmResponse.OutputTokens, cost);
        }
    }
}
