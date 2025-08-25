using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Chat;
using ProjectVG.Application.Services.Chat.CostTracking;
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
            services.AddSingleton<IOAuth2Service, OAuth2Service>();
            services.AddScoped<IOAuth2ProviderFactory, OAuth2ProviderFactory>();

            // User Services
            services.AddScoped<IUserService, UserService>();

            // Character Services
            services.AddScoped<ICharacterService, CharacterService>();

            // Chat Services
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IChatMetricsService, ChatMetricsService>();

            // Conversation Services
            services.AddScoped<IConversationService, ConversationService>();

            // Session Services
            services.AddScoped<IConnectionRegistry, ConnectionRegistry>();

            // WebSocket Services
            services.AddScoped<IWebSocketManager, WebSocketManager>();

            return services;
        }
    }
}
