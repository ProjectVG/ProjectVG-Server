using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.LLMClient;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using ProjectVG.Infrastructure.Integrations.TextToSpeechClient;
using ProjectVG.Infrastructure.Persistence.EfCore;
using ProjectVG.Infrastructure.Persistence.Repositories.Characters;
using ProjectVG.Infrastructure.Persistence.Repositories.Conversation;
using ProjectVG.Infrastructure.Persistence.Repositories.Users;
using ProjectVG.Infrastructure.Persistence.Session;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Common.Configuration;
using StackExchange.Redis;


using Microsoft.EntityFrameworkCore;

namespace ProjectVG.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        /// <summary>
        /// Infrastructure 모듈 서비스 등록
        /// </summary>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            AddDatabaseServices(services, configuration);
            AddExternalApiClients(services, configuration);
            AddPersistenceServices(services);
            AddAuthServices(services, configuration);
            AddRedisServices(services, configuration);

            return services;
        }

        /// <summary>
        /// 데이터베이스 마이그레이션 실행
        /// </summary>
        public static IServiceProvider MigrateDatabase(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ProjectVGDbContext>();
            context.Database.Migrate();
            return serviceProvider;
        }

        /// <summary>
        /// 데이터베이스 서비스
        /// </summary>
        private static void AddDatabaseServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ProjectVGDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        }

        /// <summary>
        /// 외부 API 클라이언트
        /// </summary>
        private static void AddExternalApiClients(IServiceCollection services, IConfiguration configuration)
        {
            var llmBaseUrl = configuration.GetValue<string>("LLM:BaseUrl") ?? Environment.GetEnvironmentVariable("LLM_BASE_URL") ?? "http://localhost:5601";
            var memoryBaseUrl = configuration.GetValue<string>("MEMORY:BaseUrl") ?? Environment.GetEnvironmentVariable("MEMORY_BASE_URL") ?? "http://localhost:5602";

            services.AddHttpClient<ILLMClient, LLMClient>(client => {
                client.BaseAddress = new Uri(llmBaseUrl);
            });

            services.AddHttpClient<IMemoryClient, VectorMemoryClient>(client => {
                client.BaseAddress = new Uri(memoryBaseUrl);
            });

            services.AddHttpClient<ITextToSpeechClient, TextToSpeechClient>((sp, client) => {
                client.BaseAddress = new Uri("https://supertoneapi.com");

                var apiKey = configuration.GetValue<string>("TTSApiKey") ?? Environment.GetEnvironmentVariable("TTS_API_KEY");

                if (!string.IsNullOrWhiteSpace(apiKey)) {
                    client.DefaultRequestHeaders.Add("x-sup-api-key", apiKey);
                }
            })
            .AddTypedClient((httpClient, sp) => {
                var logger = sp.GetRequiredService<ILogger<TextToSpeechClient>>();
                return new TextToSpeechClient(httpClient, logger);
            });
        }

        /// <summary>
        /// 저장소 서비스
        /// </summary>
        private static void AddPersistenceServices(IServiceCollection services)
        {
            services.AddScoped<ICharacterRepository, SqlServerCharacterRepository>();
            services.AddScoped<IConversationRepository, SqlServerConversationRepository>();
            services.AddScoped<IUserRepository, SqlServerUserRepository>();
            services.AddSingleton<ISessionStorage, InMemorySessionStorage>();
        }

        /// <summary>
        /// 인증 서비스
        /// </summary>
        private static void AddAuthServices(IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings
            {
                Key = Environment.GetEnvironmentVariable("JWT_KEY") ?? "your-super-secret-key-with-at-least-32-characters",
                Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "ProjectVG",
                Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "ProjectVG",
                AccessTokenExpirationMinutes = configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15),
                RefreshTokenExpirationMinutes = configuration.GetValue<int>("Jwt:RefreshTokenExpirationMinutes", 1440)
            };

            services.AddSingleton(jwtSettings);
            services.AddScoped<IJwtProvider, JwtProvider>(sp => 
                new JwtProvider(jwtSettings.Key, jwtSettings.Issuer, jwtSettings.Audience, jwtSettings.AccessTokenExpirationMinutes, jwtSettings.RefreshTokenExpirationMinutes));
            
            services.AddScoped<ITokenService, TokenService>();
        }

        /// <summary>
        /// Redis 서비스
        /// </summary>
        private static void AddRedisServices(IServiceCollection services, IConfiguration configuration)
        {
            var redisConnectionString = configuration.GetConnectionString("Redis") ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? "localhost:6379";
            
            services.AddSingleton<IConnectionMultiplexer>(sp => 
                ConnectionMultiplexer.Connect(redisConnectionString));
            
            services.AddScoped<IRefreshTokenStorage, RedisRefreshTokenStorage>();
        }
    }
}
