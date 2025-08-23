using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.User;
using ProjectVG.Application.Services.Chat;
using ProjectVG.Application.Services.Chat.CostTracking;
using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Application.Services.Chat.Processors;
using ProjectVG.Application.Services.Chat.Validators;
using ProjectVG.Application.Services.Chat.Handlers;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Session;
using ProjectVG.Application.Services.Auth;

namespace ProjectVG.Application
{
    public static class ApplicationServiceCollectionExtensions
    {
        /// <summary>
        /// 애플리케이션 서비스 등록
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ICharacterService, CharacterService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ChatRequestValidator>();
            services.AddScoped<ChatLLMProcessor>();
            services.AddScoped<ChatTTSProcessor>();
            services.AddScoped<ChatResultProcessor>();
            services.AddScoped<UserInputAnalysisProcessor>();
            services.AddScoped<UserInputActionProcessor>();
            services.AddScoped<MemoryContextPreprocessor>();
            services.AddScoped<ChatFailureHandler>();

            services.AddScoped<IWebSocketManager, WebSocketManager>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IConversationService, ConversationService>();
            services.AddSingleton<IConnectionRegistry, ConnectionRegistry>();
            
            // Auth 서비스 등록
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            
            services.AddScoped<IChatMetricsService, ChatMetricsService>();

            // 비용 추적 데코레이터 등록
            services.AddCostTrackingDecorator<ChatLLMProcessor>("LLM_Processing");
            services.AddCostTrackingDecorator<ChatTTSProcessor>("TTS_Processing");
            services.AddCostTrackingDecorator<UserInputAnalysisProcessor>("User_Input_Analysis");

            return services;
        }
    }
}
