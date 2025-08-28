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
        /// <summary>
        /// 인프라스트럭처 관련 서비스를 DI 컨테이너에 등록합니다.
        /// </summary>
        /// <remarks>
        /// 데이터베이스, 외부 API 클라이언트, 퍼시스턴스, 인증(JWT), Redis 및 OAuth2 관련 서비스를 순차적으로 등록합니다.
        /// </remarks>
        /// <returns>구성된 IServiceCollection 인스턴스</returns>
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
        /// <summary>
        /// 인프라스트럭처의 영속성 관련 서비스들을 DI 컨테이너에 등록합니다.
        /// </summary>
        /// <remarks>
        /// 등록 항목 및 수명주기:
        /// - ICharacterRepository -> SqlServerCharacterRepository (Scoped)
        /// - IConversationRepository -> SqlServerConversationRepository (Scoped)
        /// - IUserRepository -> SqlServerUserRepository (Scoped)
        /// - ISessionStorage -> InMemorySessionStorage (Singleton)
        /// </remarks>
        private static void AddPersistenceServices(IServiceCollection services)
        {
            services.AddScoped<ICharacterRepository, SqlServerCharacterRepository>();
            services.AddScoped<IConversationRepository, SqlServerConversationRepository>();
            services.AddScoped<IUserRepository, SqlServerUserRepository>();
            services.AddSingleton<ISessionStorage, InMemorySessionStorage>();
        }

        /// <summary>
        /// 인증 서비스
        /// <summary>
        /// JWT 서명 키와 관련 인증 서비스를 DI 컨테이너에 등록합니다.
        /// </summary>
        /// <remarks>
        /// 다음 순서로 JWT 설정을 구성하고 서비스들을 등록합니다:
        /// - 서명 키를 우선순위로 환경변수 및 구성에서 조회:
        ///   Environment: JWT_SECRET_KEY, JWT_KEY 또는 구성값: JWT_SECRET_KEY, Jwt:SecretKey 등을 확인하며 기본값은
        ///   "your-super-secret-jwt-key-here-minimum-32-characters"입니다.
        /// - 값이 "${VAR_NAME}" 형태인 경우 내부 이름(VAR_NAME)으로 환경변수를 재조회하여 치환합니다.
        /// - Jwt 설정(JwtSettings)을 구성에서 바인드하거나, 값이 없으면 조회한 키와 기본 Issuer/Audience("ProjectVG") 및
        ///   만료값(액세스 기본 15분, 리프레시 기본 1440분)으로 생성합니다.
        /// - 구성된 JwtSettings를 싱글턴으로 등록하고, IJwtProvider(Scoped)와 ITokenService(Scoped)를 등록합니다.
        /// - 동일한 서명 키로 JwtService를 싱글턴으로 등록합니다.
        /// </remarks>
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
        /// <summary>
        /// "OAuth2" 구성 섹션을 읽어 OAuth2ProviderSettings에 바인딩하고 DI에 등록합니다.
        /// </summary>
        /// <remarks>
        /// 앱 설정의 "OAuth2" 섹션을 OAuth2ProviderSettings 타입으로 구성 바인딩하여 서비스에서 해당 설정을 주입받아 사용할 수 있게 합니다.
        /// </remarks>
        private static void AddOAuth2Services(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<OAuth2ProviderSettings>(configuration.GetSection("OAuth2"));
        }

        /// <summary>
        /// Redis 서비스 (개발 환경에서는 In-Memory 사용)
        /// <summary>
        /// Redis 관련 서비스를 DI 컨테이너에 등록합니다.
        /// </summary>
        /// <remarks>
        /// - ASPNETCORE_ENVIRONMENT 환경 변수가 "Production"(대소문자 구분 없음)일 경우:
        ///   - 설정의 ConnectionStrings:Redis 또는 환경 변수 REDIS_CONNECTION_STRING 또는 기본값 "localhost:6379" 순으로 Redis 연결 문자열을 결정합니다.
        ///   - IConnectionMultiplexer를 싱글턴으로 등록하고, AbortOnConnectFail=false, ConnectRetry=5, ReconnectRetryPolicy=ExponentialRetry(5000) 옵션으로 연결합니다.
        ///   - IRefreshTokenStorage는 RedisRefreshTokenStorage로 스코프 등록합니다.
        /// - Production이 아닐 경우 IRefreshTokenStorage는 InMemoryRefreshTokenStorage로 스코프 등록합니다.
        /// </remarks>
        private static void AddRedisServices(IServiceCollection services, IConfiguration configuration)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            
            if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                // 프로덕션에서는 Redis 사용
                var redisConnectionString = configuration.GetConnectionString("Redis") ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? "localhost:6379";
                
                services.AddSingleton<IConnectionMultiplexer>(sp => 
                {
                    var options = ConfigurationOptions.Parse(redisConnectionString);
                    options.AbortOnConnectFail = false;
                    options.ConnectRetry = 5;
                    options.ReconnectRetryPolicy = new ExponentialRetry(5000);
                    return ConnectionMultiplexer.Connect(options);
                });
                
                services.AddScoped<IRefreshTokenStorage, RedisRefreshTokenStorage>();
            }
            else
            {
                // 개발 환경에서는 In-Memory 사용
                services.AddScoped<IRefreshTokenStorage, InMemoryRefreshTokenStorage>();
            }
        }
    }
}
