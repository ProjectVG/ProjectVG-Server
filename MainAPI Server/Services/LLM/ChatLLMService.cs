using MainAPI_Server.Clients.LLM;
using MainAPI_Server.Models.External.LLM;
using MainAPI_Server.Models.Service.Chats;
using MainAPI_Server.Config;

namespace MainAPI_Server.Services.LLM
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

        public async Task<ChatResultDto> CreateTextResponseAsync(ChatContextDto contextDto)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // LLMRequest 생성
                var request = new LLMRequest
                {
                    SystemMessage = contextDto.SystemMessage,
                    UserMessage = contextDto.UserMessage,
                    Instructions = contextDto.LLMSettings.Instructions,
                    MemoryContext = contextDto.MemoryContext ?? new List<string>(),
                    ConversationHistory = contextDto.ConversationContext ?? new List<string>(),
                    MaxTokens = contextDto.LLMSettings.MaxTokens,
                    Temperature = contextDto.LLMSettings.Temperature,
                    Model = contextDto.LLMSettings.Model
                };

                // LLM 호출
                var llmResponse = await _llmClient.SendRequestAsync(request);

                var endTime = DateTime.UtcNow;
                var processingTime = (endTime - startTime).TotalMilliseconds;

                // ChatResultDto 생성 및 반환
                return new ChatResultDto
                {
                    SessionId = contextDto.SessionId,
                    UserMessage = contextDto.UserMessage,
                    AiResponse = llmResponse.Success ? llmResponse.Response : $"오류로 답변 생성 실패",
                    IsSuccess = llmResponse.Success,
                    TokensUsed = llmResponse.TokensUsed,
                    ErrorMessage = llmResponse.Success ? null : llmResponse.Response,
                    SelectedModel = contextDto.LLMSettings.Model,
                    ProcessingTimeMs = processingTime,
                    ContextInfo = contextDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM 서비스 오류 발생");
                
                return new ChatResultDto
                {
                    SessionId = contextDto.SessionId,
                    UserMessage = contextDto.UserMessage,
                    AiResponse = "서비스 오류로 답변 생성 실패",
                    IsSuccess = false,
                    TokensUsed = 0,
                    ErrorMessage = ex.Message,
                    SelectedModel = contextDto.LLMSettings.Model,
                    ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
                    ContextInfo = contextDto
                };
            }
        }
    }
} 