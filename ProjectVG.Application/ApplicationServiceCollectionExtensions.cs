using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
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
using ProjectVG.Application.Services.Credit;
using ProjectVG.Application.Services.Users;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Application.Services.MessageBroker;
using ProjectVG.Application.Services.Server;

namespace ProjectVG.Application
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Auth Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IOAuth2AuthService, OAuth2AuthService>();
            services.AddScoped<IOAuth2Service, OAuth2Service>();
            services.AddScoped<IOAuth2CodeValidator, OAuth2CodeValidator>();
            services.AddScoped<IOAuth2UserService, OAuth2UserService>();
            services.AddScoped<IOAuth2AccountManager, OAuth2AccountManager>();
            services.AddScoped<IOAuth2ProviderFactory, OAuth2ProviderFactory>();

            // User Services
            services.AddScoped<IUserService, UserService>();

            // Character Services
            services.AddScoped<ICharacterService, CharacterService>();

            // Credit Management Services
            services.AddScoped<ICreditManagementService, CreditManagementService>();

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

            // Distributed System Services
            AddDistributedServices(services, configuration);

            return services;
        }

        /// <summary>
        /// 분산 시스템 관련 서비스 등록
        /// </summary>
        private static void AddDistributedServices(IServiceCollection services, IConfiguration configuration)
        {
            var distributedEnabled = configuration.GetValue<bool>("DistributedSystem:Enabled", false);

            if (distributedEnabled)
            {
                // 분산 환경 서비스
                services.AddSingleton<IMessageBroker, DistributedMessageBroker>();
                services.AddSingleton<IWebSocketManager, DistributedWebSocketManager>();
            }
            else
            {
                // 단일 서버 환경 서비스
                services.AddSingleton<IMessageBroker, LocalMessageBroker>();
                services.AddSingleton<IWebSocketManager, WebSocketManager>();
            }

            // WebSocket 연결 관리
            services.AddSingleton<IConnectionRegistry, ConnectionRegistry>();
        }
    }
}
