using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Domain.Enums;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.MemoryClient;

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
            await _conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.User, context.UserMessage);
            await _conversationService.AddMessageAsync(context.UserId, context.CharacterId, ChatRole.Assistant, result.Response);
            await _memoryClient.AddMemoryAsync(context.MemoryStore, result.Response);
        }
    }
} 