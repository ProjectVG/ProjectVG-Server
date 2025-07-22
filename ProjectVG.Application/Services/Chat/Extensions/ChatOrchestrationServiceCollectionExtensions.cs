using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Application.Services.Chat.Core;
using ProjectVG.Application.Services.Chat.Handlers;
using ProjectVG.Application.Services.Chat.Preprocessors;

namespace ProjectVG.Application.Services.Chat.Extensions
{
    public static class ChatOrchestrationServiceCollectionExtensions
    {
        public static IServiceCollection AddChatOrchestrationServices(this IServiceCollection services)
        {
            services.AddScoped<ChatOrchestrator>();
            services.AddScoped<ChatPreprocessor>();
            services.AddScoped<LLMHandler>();
            services.AddScoped<TTSHandler>();
            services.AddScoped<ResultSender>();
            services.AddScoped<ResultPersister>();
            services.AddScoped<MemoryContextPreprocessor>();
            services.AddScoped<ConversationHistoryPreprocessor>();
            services.AddScoped<InstructionPreprocessor>();
            return services;
        }
    }
} 