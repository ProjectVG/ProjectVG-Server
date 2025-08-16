using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.User;
using ProjectVG.Application.Services.Chat;
using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Application.Services.Chat.Processors;
using ProjectVG.Application.Services.Chat.Validators;
using ProjectVG.Application.Services.WebSocket;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Session;

namespace ProjectVG.Application
{
    public static class ApplicationServiceCollectionExtensions
    {
        /// <summary>
        /// 애플리케이션 서비스 등록
        /// <summary>
        /// 애플리케이션 계층의 서비스들을 DI 컨테이너에 등록하고 구성된 IServiceCollection을 반환합니다.
        /// </summary>
        /// <remarks>
        /// 대부분의 서비스는 Scoped로 등록되며, IConnectionRegistry는 Singleton으로 등록됩니다.
        /// </remarks>
        /// <returns>등록이 완료된 동일한 IServiceCollection 인스턴스(메서드 체이닝용).</returns>
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

            services.AddScoped<IWebSocketManager, WebSocketManager>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IConversationService, ConversationService>();
            services.AddSingleton<IConnectionRegistry, ConnectionRegistry>();

            return services;
        }
    }
}
