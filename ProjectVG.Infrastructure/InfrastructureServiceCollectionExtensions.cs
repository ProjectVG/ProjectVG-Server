using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using ProjectVG.Infrastructure.Integrations.LLMClient;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using ProjectVG.Infrastructure.Integrations.TextToSpeechClient;
using ProjectVG.Infrastructure.Persistence.EfCore;
using ProjectVG.Infrastructure.Persistence.Repositories.Characters;
using ProjectVG.Infrastructure.Persistence.Repositories.Conversation;
using ProjectVG.Infrastructure.Persistence.Repositories.Token;
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
            AddOAuth2Services(services, configuration);

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
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), 
                    sqlOptions => sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null)));
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
            services.AddScoped<ITokenTransactionRepository, SqlServerTokenTransactionRepository>();
            services.AddSingleton<ISessionStorage, InMemorySessionStorage>();
        }

        /// <summary>
        /// 인증 서비스
        /// </summary>
        private static void AddAuthServices(IServiceCollection services, IConfiguration configuration)
        {
            // JWT 키를 여러 소스에서 찾기
            var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? 
                        configuration["JWT_SECRET_KEY"] ?? 
                        configuration["JWT:SecretKey"] ??
                        Environment.GetEnvironmentVariable("JWT_KEY") ?? 
                        "your-super-secret-jwt-key-here-minimum-32-characters";
            
            // 환경변수 치환 문자열이 그대로 남아있는 경우 처리
            if (jwtKey.StartsWith("${") && jwtKey.EndsWith("}"))
            {
                var envVarName = jwtKey.Substring(2, jwtKey.Length - 3);
                jwtKey = Environment.GetEnvironmentVariable(envVarName) ?? 
                        "your-super-secret-jwt-key-here-minimum-32-characters";
            }
            
            var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings
            {
                Key = jwtKey,
                Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "ProjectVG",
                Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "ProjectVG",
                AccessTokenExpirationMinutes = configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15),
                RefreshTokenExpirationMinutes = configuration.GetValue<int>("Jwt:RefreshTokenExpirationMinutes", 1440)
            };

            services.AddSingleton(jwtSettings);
            services.AddScoped<IJwtProvider, JwtProvider>(sp => 
                new JwtProvider(jwtKey, jwtSettings.Issuer, jwtSettings.Audience, jwtSettings.AccessTokenExpirationMinutes, jwtSettings.RefreshTokenExpirationMinutes));
            
            services.AddScoped<ITokenService, TokenService>();

            // OAuth2 JWT 서비스 추가 (동일한 jwtKey 사용)
            services.AddSingleton(new JwtService(jwtKey));
        }

        /// <summary>
        /// OAuth2 서비스
        /// </summary>
        private static void AddOAuth2Services(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<OAuth2ProviderSettings>(configuration.GetSection("OAuth2"));
        }

        /// <summary>
        /// Redis 및 분산 캐시 서비스
        /// </summary>
        private static void AddRedisServices(IServiceCollection services, IConfiguration configuration)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var redisConnectionString = configuration.GetConnectionString("Redis") ?? 
                                      Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? 
                                      "localhost:6380";
            
            // Redis 연결 시도 (환경 무관하게 시도)
            try
            {
                // Redis 연결 테스트
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false;
                options.ConnectRetry = 3;
                options.ConnectTimeout = 5000;
                options.ReconnectRetryPolicy = new ExponentialRetry(5000);
                
                var multiplexer = ConnectionMultiplexer.Connect(options);
                
                // Redis 사용 가능한 경우
                services.AddSingleton<IConnectionMultiplexer>(multiplexer);
                services.AddStackExchangeRedisCache(opt =>
                {
                    opt.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(multiplexer);
                });
                services.AddScoped<IRefreshTokenStorage, RedisRefreshTokenStorage>();
                
                Console.WriteLine($"Redis 연결 성공: {redisConnectionString}");
            }
            catch (Exception ex)
            {
                // Redis 연결 실패 시 In-Memory 대체
                Console.WriteLine($"Redis 연결 실패, In-Memory로 대체: {ex.Message}");
                services.AddDistributedMemoryCache();
                services.AddScoped<IRefreshTokenStorage, InMemoryRefreshTokenStorage>();
            }
        }
    }
}
