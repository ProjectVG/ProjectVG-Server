using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.User;
using ProjectVG.Application.Services.Chat;
using ProjectVG.Application.Services.Chat.Preprocessors;

namespace ProjectVG.Application.Services
{
    /// <summary>
    /// 애플리케이션 서비스 DI 등록 확장 메서드
    /// </summary>
    public static class ApplicationServiceCollectionExtensions
    {
        /// <summary>
        /// 외부 API 경계에 노출되는 핵심 애플리케이션 서비스를 등록합니다
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
            
            // 전처리기들 등록
            services.AddScoped<MemoryContextPreprocessor>();
            services.AddScoped<ConversationHistoryPreprocessor>();
            
            return services;
        }
    }
}


