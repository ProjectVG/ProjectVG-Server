using ProjectVG.Common.Configuration;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.LLMClient;
using ProjectVG.Infrastructure.Integrations.LLMClient.Models;

namespace ProjectVG.Application.Services.LLM
{
    public class ChatLLMService : ILLMService
    {
        private readonly ILLMClient _llmClient;
        private readonly ILogger<ChatLLMService> _logger;

        public ChatLLMService(ILLMClient llmClient, ILogger<ChatLLMService> logger) 
        {
            _llmClient = llmClient;
            _logger = logger;
        }

        public async Task<LLMResponse> CreateTextResponseAsync(
            string systemMessage,
            string userMessage,
            string instructions,
            List<string> memoryContext,
            List<string> conversationHistory,
            int maxTokens,
            float temperature,
            string model)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // LLMRequest 생성
                var request = new LLMRequest
                {
                    SystemMessage = systemMessage,
                    UserMessage = userMessage,
                    Instructions = instructions,
                    MemoryContext = memoryContext ?? new List<string>(),
                    ConversationHistory = conversationHistory ?? new List<string>(),
                    MaxTokens = maxTokens,
                    Temperature = temperature,
                    Model = model
                };

                // LLMClient 호출
                var llmResponse = await _llmClient.SendRequestAsync(request);

                var endTime = DateTime.UtcNow;
                var processingTime = (endTime - startTime).TotalMilliseconds;

                _logger.LogInformation("LLMClient 응답 생성 완료: 토큰 사용량 ({TokensUsed}), 요청 시간({ProcessingTimeMs:F2}ms)", 
                    llmResponse.TokensUsed, processingTime);

                return llmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLMClient 서비스 오류 발생");
                
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Response = "서비스 오류로 답변 생성 실패",
                    TokensUsed = 0,
                    ResponseTime = (DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
        }
    }
} 