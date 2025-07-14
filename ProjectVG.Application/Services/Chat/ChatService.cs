using ProjectVG.Application.Services.LLM;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Session;
using ProjectVG.Infrastructure.ExternalApis.MemoryClient;
using ProjectVG.Common.Configuration;
using ProjectVG.Application.Models.Chat;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Enums;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly ILLMService _llmService;
        private readonly ICharacterService _characterService;
        private readonly IConversationService _conversationService;
        private readonly ISessionService _sessionService;
        private readonly IMemoryClient _memoryClient;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            ILLMService llmService,
            ICharacterService characterService,
            IConversationService conversationService,
            ISessionService sessionService,
            IMemoryClient memoryClient,
            ILogger<ChatService> logger)
        {
            _llmService = llmService;
            _characterService = characterService;
            _conversationService = conversationService;
            _sessionService = sessionService;
            _memoryClient = memoryClient;
            _logger = logger;
        }

        public async Task ProcessChatRequestAsync(ProcessChatCommand command)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogInformation("채팅 요청 처리 시작: 세션 {SessionId}, 메시지: {Message}", command.SessionId, command.Message);

                // 1. 메모리 컨텍스트 수집
                var memoryContext = await CollectMemoryContextAsync(command.Message);

                // 2. 대화 기록 수집
                var conversationHistory = await CollectConversationHistoryAsync(command.SessionId);

                // 3. 시스템 메시지 준비
                var systemMessage = await PrepareSystemMessageAsync(command.CharacterId);

                // 4. LLMClient 응답 생성
                var llmResponse = await _llmService.CreateTextResponseAsync(
                    systemMessage,
                    command.Message,
                    LLMSettings.Chat.Instructions,
                    memoryContext,
                    conversationHistory,
                    LLMSettings.Chat.MaxTokens,
                    LLMSettings.Chat.Temperature,
                    LLMSettings.Chat.Model
                );

                // 5. 대화 기록 저장
                await SaveConversationAsync(command.SessionId, command.Message, llmResponse.Response);

                // 6. 메모리에 저장 (비동기)
                _ = Task.Run(async () => await SaveToMemoryAsync(command.Message, llmResponse.Response));

                // 7. WebSocket으로 응답 전송
                await SendResponseToClientAsync(command.SessionId, llmResponse.Response);

                var endTime = DateTime.UtcNow;
                var processingTime = (endTime - startTime).TotalMilliseconds;

                _logger.LogInformation("채팅 요청 처리 완료: 세션 {SessionId}, 소요시간: {ProcessingTime:F2}ms, 토큰: {TokensUsed}", 
                    command.SessionId, processingTime, llmResponse.TokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 요청 처리 중 오류 발생: 세션 {SessionId}", command.SessionId);
                
                // 오류 응답을 클라이언트에게 전송
                await SendResponseToClientAsync(command.SessionId, "서비스 오류로 답변 생성 실패");
            }
        }

        /// <summary>
        /// 메모리 컨텍스트 수집
        /// </summary>
        private async Task<List<string>> CollectMemoryContextAsync(string userMessage)
        {
            try
            {
                var searchResults = await _memoryClient.SearchAsync(userMessage, 3);
                return searchResults.Select(r => r.Text).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "메모리 컨텍스트 수집 실패");
                return new List<string>();
            }
        }

        /// <summary>
        /// 대화 기록 수집
        /// </summary>
        private async Task<List<string>> CollectConversationHistoryAsync(string sessionId)
        {
            try
            {
                var history = await _conversationService.GetConversationHistoryAsync(sessionId, 10);
                return history.Select(h => $"{h.Role}: {h.Content}").ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "대화 기록 수집 실패: 세션 {SessionId}", sessionId);
                return new List<string>();
            }
        }

        /// <summary>
        /// 시스템 메시지 준비
        /// </summary>
        private async Task<string> PrepareSystemMessageAsync(Guid? characterId)
        {
            if (characterId.HasValue)
            {
                var character = await _characterService.GetCharacterByIdAsync(characterId.Value);
                if (character != null)
                {
                    return character.Role;
                }
            }

            return LLMSettings.Chat.SystemPrompt;
        }

        /// <summary>
        /// 대화 기록 저장
        /// </summary>
        private async Task SaveConversationAsync(string sessionId, string userMessage, string aiResponse)
        {
            try
            {
                await _conversationService.AddMessageAsync(sessionId, ChatRole.User, userMessage);
                await _conversationService.AddMessageAsync(sessionId, ChatRole.Assistant, aiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "대화 기록 저장 실패: 세션 {SessionId}", sessionId);
            }
        }

        /// <summary>
        /// 메모리에 저장
        /// </summary>
        private async Task SaveToMemoryAsync(string userMessage, string aiResponse)
        {
            try
            {
                await _memoryClient.AddMemoryAsync(userMessage);
                await _memoryClient.AddMemoryAsync(aiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메모리 저장 실패");
            }
        }

        /// <summary>
        /// WebSocket으로 클라이언트에게 응답 전송
        /// </summary>
        private async Task SendResponseToClientAsync(string sessionId, string response)
        {
            try
            {
                await _sessionService.SendMessageAsync(sessionId, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "클라이언트에게 응답 전송 실패: 세션 {SessionId}", sessionId);
            }
        }
    }
} 