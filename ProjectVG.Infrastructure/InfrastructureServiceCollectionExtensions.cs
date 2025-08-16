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


using Microsoft.EntityFrameworkCore;

namespace ProjectVG.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        /// <summary>
        /// Infrastructure 모듈 서비스 등록
        /// <summary>
        /// 프로젝트의 인프라스트럭처(데이터베이스, 외부 API 클라이언트, 영속성)를 DI 컨테이너에 등록합니다.
        /// </summary>
        /// <remarks>
        /// 내부적으로 데이터베이스, 외부 API 클라이언트(LLM, Memory, Text-to-Speech) 및 리포지토리/세션 구현을 등록하는 세부 등록 메서드를 호출합니다.
        /// </remarks>
        /// <returns>구성된 동일한 <see cref="IServiceCollection"/> 인스턴스.</returns>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            AddDatabaseServices(services, configuration);
            AddExternalApiClients(services, configuration);
            AddPersistenceServices(services);

            return services;
        }

        /// <summary>
        /// 데이터베이스 마이그레이션 실행
        /// <summary>
        /// 주어진 IServiceProvider에서 새 범위를 생성하고 데이터베이스 마이그레이션을 적용한 뒤 원래 IServiceProvider를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 스코프 내에서 ProjectVGDbContext를 요청하여 EF Core의 <c>Database.Migrate()</c>를 실행합니다. 애플리케이션 시작 시 호출하여 데이터베이스 스키마를 최신 상태로 유지하는 데 사용됩니다.
        /// </remarks>
        /// <returns>원래의 <see cref="IServiceProvider"/> 인스턴스.</returns>
        public static IServiceProvider MigrateDatabase(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ProjectVGDbContext>();
            context.Database.Migrate();
            return serviceProvider;
        }

        /// <summary>
        /// 데이터베이스 서비스
        /// <summary>
        /// EF Core ProjectVGDbContext를 SQL Server에 연결되도록 DI 컨테이너에 등록합니다.
        /// </summary>
        /// <remarks>
        /// 데이터베이스 연결 문자열은 IConfiguration에서 "DefaultConnection" 키를 사용하여 읽어옵니다.
        /// </remarks>
        private static void AddDatabaseServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ProjectVGDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        }

        /// <summary>
        /// 외부 API 클라이언트
        /// <summary>
        /// 외부 API용 HTTP 클라이언트들을 DI 컨테이너에 등록합니다.
        /// </summary>
        /// <remarks>
        /// 등록되는 클라이언트:
        /// - ILLMClient (LLMClient): BaseAddress는 설정키 "LLM:BaseUrl", 환경변수 "LLM_BASE_URL", 또는 기본값 "http://localhost:5601" 순으로 결정됩니다.
        /// - IMemoryClient (VectorMemoryClient): BaseAddress는 설정키 "MEMORY:BaseUrl", 환경변수 "MEMORY_BASE_URL", 또는 기본값 "http://localhost:5602" 순으로 결정됩니다.
        /// - ITextToSpeechClient (TextToSpeechClient): BaseAddress는 "https://supertoneapi.com"으로 고정되며, 설정키 "TTSApiKey" 또는 환경변수 "TTS_API_KEY"가 존재하면 요청 헤더 "x-sup-api-key"에 추가됩니다. TextToSpeechClient는 Typed Client 방식으로 ILogger<TextToSpeechClient>를 주입받아 생성됩니다.
        /// </remarks>
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
        /// 퍼시스턴스 관련 서비스(리포지토리 및 세션 저장소)를 의존성 주입 컨테이너에 등록합니다.
        /// </summary>
        /// <remarks>
        /// 등록되는 서비스와 수명:
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
    }
}
