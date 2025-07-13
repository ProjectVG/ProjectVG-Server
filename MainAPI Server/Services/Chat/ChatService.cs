using MainAPI_Server.Clients.LLM;
using MainAPI_Server.Clients.Memory;
using MainAPI_Server.Models.Chat;
using MainAPI_Server.Models.External.MemorySrore;
using MainAPI_Server.Models.Request;
using MainAPI_Server.Models.DTOs.Chat;
using MainAPI_Server.Services.Conversation;
using MainAPI_Server.Services.LLM;
using MainAPI_Server.Services.Session;
using MainAPI_Server.Config;

namespace MainAPI_Server.Services.Chat
{
    public interface IChatService
    {
        Task ProcessChatRequestAsync(ChatRequest request);
    }

    public class ChatService : IChatService
    {
        private readonly IMemoryStoreClient _memoryStoreClient;
        private readonly ILLMService _llmService ;
        private readonly IConversationService _conversationService;
        private readonly ISessionManager _sessionManager;

        public ChatService(IMemoryStoreClient memoryStoreClient, ILLMService llmService, IConversationService conversationService, ISessionManager sessionManager)
        {
            _memoryStoreClient = memoryStoreClient;
            _llmService = llmService;
            _conversationService = conversationService;
            _sessionManager = sessionManager;
        }

        public async Task ProcessChatRequestAsync(ChatRequest request)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // [1] 데이터 전처리 단계
                var contextDto = await PrepareChatContextAsync(request);

                // [2] LLM 서비스 호출 (DTO 기반)
                var resultDto = await _llmService.CreateTextResponseAsync(contextDto);

                // [3] 후처리 단계
                await ProcessAfterLLMAsync(resultDto);

                // [4] 클라이언트 응답 전송
                await SendResponseToClientAsync(resultDto);

                var endTime = DateTime.UtcNow;
                var processingTime = (endTime - startTime).TotalMilliseconds;

                Console.WriteLine($"Chat 요청 완료: {request.SessionId}, 소요시간: {processingTime:F2}ms, 사용된 토큰: {resultDto.TokensUsed}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chat 요청 오류: {ex.Message}");
                await _sessionManager.SendToClientAsync(request.SessionId, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 채팅 컨텍스트 준비 (데이터 전처리)
        /// </summary>
        private async Task<ChatContextDto> PrepareChatContextAsync(ChatRequest request)
        {
            // [1] 장기 기억 검색 수행
            List<MemorySearchResult> memorySearchResults = await _memoryStoreClient.SearchAsync(request.Message);
            List<string> memoryContext = memorySearchResults.Select(r => r.Text).ToList();

            // [2] 최근 대화 기록 가져오기
            var recentMessages = _conversationService.GetConversationHistory(request.SessionId, 10);
            List<string> conversationContext = recentMessages.Select(m => $"{m.Role}: {m.Content}").ToList();

            // [3] ChatContextDto 생성
            return new ChatContextDto
            {
                SessionId = request.SessionId,
                UserMessage = request.Message,
                Actor = request.Actor,
                Action = request.Action,
                SystemMessage = LLMSettings.Chat.SystemPrompt,
                ConversationContext = conversationContext,
                MemoryContext = memoryContext,
                LLMSettings = new LLMSettingsDto
                {
                    MaxTokens = LLMSettings.Chat.MaxTokens,
                    Temperature = LLMSettings.Chat.Temperature,
                    Model = LLMSettings.Chat.Model,
                    Instructions = LLMSettings.Chat.Instructions
                }
            };
        }

        /// <summary>
        /// LLM 처리 후 작업 수행 (후처리)
        /// </summary>
        private async Task ProcessAfterLLMAsync(ChatResultDto resultDto)
        {
            // [1] 사용자 메시지 & 응답 저장
            _conversationService.AddMessage(resultDto.SessionId, MessageRole.User, resultDto.UserMessage);
            _conversationService.AddMessage(resultDto.SessionId, MessageRole.Assistant, resultDto.AiResponse);

            // [2] 보이스 서버로 전송 (주석 처리됨)
            // await _voiceClient.SendToVoiceServerAsync(resultDto);

            // [3] 대화 내용을 장기 기억에 저장 (비동기 처리)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _memoryStoreClient.AddMemoryAsync(resultDto.UserMessage);
                    if (resultDto.IsSuccess)
                    {
                        await _memoryStoreClient.AddMemoryAsync(resultDto.AiResponse);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"메모리 저장 실패: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 클라이언트에게 응답 전송
        /// </summary>
        private async Task SendResponseToClientAsync(ChatResultDto resultDto)
        {
            var responseMessage = $"[요청] : {resultDto.UserMessage} " +
                                $"[응답] : {resultDto.AiResponse} " +
                                $"[응답 소요 시간] : {resultDto.ProcessingTimeMs:F2}ms" +
                                $"[사용된 토큰] : {resultDto.TokensUsed}";

            await _sessionManager.SendToClientAsync(resultDto.SessionId, responseMessage);
        }
    }
} 