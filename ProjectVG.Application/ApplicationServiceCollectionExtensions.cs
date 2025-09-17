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
using ProjectVG.Application.Services.Credit;
using ProjectVG.Application.Services.Users;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Domain.Services.MessageBus;
using ProjectVG.Domain.Services.Session;

namespace ProjectVG.Application
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
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

            // Session Services
            services.AddSingleton<IConnectionRegistry, ConnectionRegistry>();

            // WebSocket Services
            services.AddScoped<IWebSocketManager, WebSocketManager>();

            return services;
        }

        /// <summary>
        /// 분산 시스템 서비스들을 추가합니다
        /// </summary>
        public static IServiceCollection AddDistributedServices(this IServiceCollection services)
        {
            // 분산 세션 관리
            services.AddSingleton<IDistributedSessionManager, Infrastructure.Session.RedisDistributedSessionManager>();

            // 분산 메시지 버스
            services.AddSingleton<IDistributedMessageBus, Infrastructure.MessageBus.RedisDistributedMessageBus>();

            // 분산 WebSocket 관리
            services.AddScoped<IDistributedWebSocketManager, DistributedWebSocketManager>();

            // 분산 채팅 결과 핸들러
            services.AddScoped<DistributedChatSuccessHandler>();

            // 메시지 버스 결과 핸들러
            services.AddScoped<Services.MessageBus.DistributedChatResultHandler>();

            // 분산 서버 관리자 (백그라운드 서비스)
            services.AddHostedService<Infrastructure.Services.DistributedServerManager>();

            return services;
        }

        /// <summary>
        /// 레거시 WebSocketManager에 분산 기능을 추가합니다
        /// 기존 코드 호환성을 위해 WebSocketManager를 분산 기능과 함께 등록합니다
        /// </summary>
        public static IServiceCollection AddDistributedWebSocketManager(this IServiceCollection services)
        {
            // 기존 WebSocketManager를 분산 기능과 함께 재등록
            services.AddScoped<IWebSocketManager>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<IWebSocketManager>>();
                var connectionRegistry = provider.GetRequiredService<IConnectionRegistry>();
                var sessionStorage = provider.GetRequiredService<Infrastructure.Persistence.Session.ISessionStorage>();
                var distributedManager = provider.GetRequiredService<IDistributedWebSocketManager>();

                return new WebSocketManager(logger, connectionRegistry, sessionStorage, distributedManager);
            });

            return services;
        }
    }
}
