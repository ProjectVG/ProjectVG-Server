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
        /// <summary>
        /// 애플리케이션의 핵심 서비스들을 의존성 주입 컨테이너에 등록합니다.
        /// </summary>
        /// <remarks>
        /// 등록 항목: 인증(Auth), 사용자 및 캐릭터 서비스, 채팅(코어/검증/전처리/프로세서/핸들러/비용 추적 데코레이터), 대화 및 세션(연결 레지스트리), WebSocket 관리 등.
        /// 각 서비스는 코드에서 지정한 수명(scope/singleton)에 따라 등록됩니다.
        /// </remarks>
        /// <returns>구성된 <see cref="IServiceCollection"/> 객체를 반환합니다.</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Auth Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddSingleton<IOAuth2Service, OAuth2Service>();
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
