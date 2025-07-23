using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Infrastructure.ExternalApis.MemoryClient;
using ProjectVG.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Chat.Handlers
{
    public class ResultPersister
    {
        private readonly IConversationService _conversationService;
        private readonly IMemoryClient _memoryClient;
        private readonly ILogger<ResultPersister> _logger;

        public ResultPersister(IConversationService conversationService, IMemoryClient memoryClient, ILogger<ResultPersister> logger)
        {
            _conversationService = conversationService;
            _memoryClient = memoryClient;
            _logger = logger;
        }

        public async Task PersistResultAsync(ChatPreprocessContext context, ChatProcessResult result)
        {
            try
            {
                // 대화 기록
                await _conversationService.AddMessageAsync(context.SessionId, ChatRole.User, context.UserMessage);
                await _conversationService.AddMessageAsync(context.SessionId, ChatRole.Assistant, result.Response);

                // 메모리 클라이언트에 동록
                await _memoryClient.AddMemoryAsync(context.UserMemory, result.Response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "결과 저장 실패: 세션 {SessionId}", context.SessionId);
            }
        }
    }
} 