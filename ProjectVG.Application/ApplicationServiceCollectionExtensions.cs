using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Chat;
using ProjectVG.Application.Services.Chat.CostTracking;
using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Application.Services.Chat.Processors;
using ProjectVG.Application.Services.Chat.Validators;
using ProjectVG.Application.Services.Chat.Handlers;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Session;
using ProjectVG.Application.Services.Users;
using ProjectVG.Application.Services.WebSocket;

namespace ProjectVG.Application
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Auth Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IOAuth2Service, OAuth2Service>();
            services.AddScoped<IOAuth2ProviderFactory, OAuth2ProviderFactory>();

            // User Services
            services.AddScoped<IUserService, UserService>();

            // Character Services
            services.AddScoped<ICharacterService, CharacterService>();

            // Chat Services - Core
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IChatMetricsService, ChatMetricsService>();

            services.AddScoped<ICharacterService, CharacterService>();
            services.AddScoped<IUserService, UserService>();

            // Chat Services - Validators
            services.AddScoped<ChatRequestValidator>();
            
            // Chat Services - Preprocessors
            services.AddScoped<MemoryContextPreprocessor>();
            services.AddScoped<UserInputAnalysisProcessor>();
            
            // Chat Services - Processors
            services.AddScoped<UserInputActionProcessor>();
            services.AddScoped<ChatLLMProcessor>();
            services.AddScoped<ChatTTSProcessor>();
            services.AddScoped<ChatResultProcessor>();

            // Chat Services - Handlers
            services.AddScoped<ChatSuccessHandler>();
            services.AddScoped<ChatFailureHandler>();
            
            // Chat Services - Cost Tracking Decorators
            services.AddCostTrackingDecorator<UserInputAnalysisProcessor>("UserInputAnalysis");
            services.AddCostTrackingDecorator<ChatLLMProcessor>("ChatLLM");
            services.AddCostTrackingDecorator<ChatTTSProcessor>("ChatTTS");

            // Conversation Services
            services.AddScoped<IConversationService, ConversationService>();

            // Session Services
            services.AddSingleton<IConnectionRegistry, ConnectionRegistry>();

            // WebSocket Services
            services.AddScoped<IWebSocketManager, WebSocketManager>();

            return services;
        }
    }
}
