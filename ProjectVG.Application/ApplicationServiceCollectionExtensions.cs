using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using ProjectVG.Application.Services.Auth;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Chat;
using ProjectVG.Application.Services.Chat.CostTracking;
using ProjectVG.Application.Services.Chat.Handlers;
using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Application.Services.Chat.Processors;
using ProjectVG.Application.Services.Chat.Validators;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Credit;
using ProjectVG.Application.Services.MessageBroker;
using ProjectVG.Application.Services.Server;
using ProjectVG.Application.Services.Session;
using ProjectVG.Application.Services.Users;
using ProjectVG.Infrastructure.Persistence.Session;
using System;

namespace ProjectVG.Application
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            AddAuthServices(services);
            AddDomainServices(services);
            AddChatServices(services);
            AddDistributedServices(services, configuration);

            return services;
        }

        private static void AddAuthServices(IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IOAuth2AuthService, OAuth2AuthService>();
            services.AddScoped<IOAuth2Service, OAuth2Service>();
            services.AddScoped<IOAuth2CodeValidator, OAuth2CodeValidator>();
            services.AddScoped<IOAuth2UserService, OAuth2UserService>();
            services.AddScoped<IOAuth2AccountManager, OAuth2AccountManager>();
            services.AddScoped<IOAuth2ProviderFactory, OAuth2ProviderFactory>();
        }

        private static void AddDomainServices(IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICharacterService, CharacterService>();
            services.AddScoped<ICreditManagementService, CreditManagementService>();
            services.AddScoped<IConversationService, ConversationService>();
        }

        private static void AddChatServices(IServiceCollection services)
        {
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IChatMetricsService, ChatMetricsService>();
            services.AddScoped<ChatRequestValidator>();

            services.AddScoped<MemoryContextPreprocessor>();
            services.AddScoped<UserInputAnalysisProcessor>();

            services.AddScoped<UserInputActionProcessor>();
            services.AddScoped<ChatLLMProcessor>();
            services.AddScoped<ChatTTSProcessor>();
            services.AddScoped<ChatResultProcessor>();

            services.AddScoped<ChatSuccessHandler>();
            services.AddScoped<ChatFailureHandler>();

            services.AddCostTrackingDecorator<UserInputAnalysisProcessor>("UserInputAnalysis");
            services.AddCostTrackingDecorator<ChatLLMProcessor>("ChatLLM");
            services.AddCostTrackingDecorator<ChatTTSProcessor>("ChatTTS");
        }

        private static void AddDistributedServices(IServiceCollection services, IConfiguration configuration)
        {
            // DistributedMessageBroker를 즉시 생성하도록 팩토리 패턴 사용
            services.AddSingleton<IMessageBroker>(serviceProvider =>
            {
                var redis = serviceProvider.GetRequiredService<IConnectionMultiplexer>();
                var connectionManager = serviceProvider.GetRequiredService<IWebSocketConnectionManager>();
                var serverRegistration = serviceProvider.GetRequiredService<ProjectVG.Domain.Services.Server.IServerRegistrationService>();
                var logger = serviceProvider.GetRequiredService<ILogger<DistributedMessageBroker>>();

                logger.LogInformation("[DI] DistributedMessageBroker 팩토리에서 생성 시작");
                var broker = new DistributedMessageBroker(redis, connectionManager, serverRegistration, logger);
                logger.LogInformation("[DI] DistributedMessageBroker 팩토리에서 생성 완료");
                return broker;
            });

            services.AddSingleton<ISessionManager>(serviceProvider =>
            {
                var sessionStorage = serviceProvider.GetService<ISessionStorage>();
                var logger = serviceProvider.GetRequiredService<ILogger<RedisSessionManager>>();
                return new RedisSessionManager(sessionStorage, logger);
            });

            AddWebSocketConnectionServices(services);

            // MessageBroker 초기화를 강제하는 HostedService 등록
            services.AddHostedService<MessageBrokerInitializationService>();
        }

        private static void AddWebSocketConnectionServices(IServiceCollection services)
        {
            services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>();
        }
    }
}
