using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChatService> _logger;

        public ChatService(IServiceScopeFactory scopeFactory, ILogger<ChatService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task EnqueueChatRequestAsync(ProcessChatCommand command)
        {
            await Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<ChatOrchestrator>();
                await orchestrator.ProcessChatRequestAsync(command);
            });
        }
    }
} 