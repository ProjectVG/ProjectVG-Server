using ProjectVG.Application.Middlewares;
using Microsoft.AspNetCore.Authentication.Negotiate;
using ProjectVG.Infrastructure.Integrations.LLMClient;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using ProjectVG.Application.Services.Chat;
using ProjectVG.Application.Services;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Session;
using ProjectVG.Application.Services.User;
using ProjectVG.Infrastructure.Persistence.Session;
using ProjectVG.Infrastructure.Integrations.TextToSpeechClient;

using ProjectVG.Api.Configuration;
using ProjectVG.Api.Services;
using ProjectVG.Api.Middleware;
using ProjectVG.Infrastructure.Realtime.WebSocketConnection;
using ProjectVG.Application.Services.Messaging;
using ProjectVG.Common.Models.Session;
using ProjectVG.Infrastructure.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cors;
using ProjectVG.Infrastructure.Persistence.Repositories.Conversation;
using ProjectVG.Infrastructure.Persistence.Repositories.Users;
using ProjectVG.Infrastructure.Persistence.Repositories.Characters;

var builder = WebApplication.CreateBuilder(args);

// 환경 변수 치환 활성화 (${ENV_VAR} 문법 지원)
builder.Configuration.AddEnvironmentVariableSubstitution(builder.Configuration);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo {
        Title = "ProjectVG API",
        Version = "v1",
        Description = "ProjectVG API Server"
    });
});

var port = builder.Configuration.GetValue<int>("Port", 7900);
builder.WebHost.ConfigureKestrel(options => {
    options.ListenAnyIP(port);
});

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

// [Authorize]가 붙은 엔드포인트만 인증을 요구하도록 설정
builder.Services.AddAuthorization(options => {
    options.FallbackPolicy = null;
});

// EF Core 설정
builder.Services.AddDbContext<ProjectVGDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// External API Clients - 환경 변수 우선, 설정 파일 차선
var llmBaseUrl = builder.Configuration.GetValueWithEnvPriority("LLM:BaseUrl", "LLM_BASE_URL", "http://localhost:5601");
var memoryBaseUrl = builder.Configuration.GetValueWithEnvPriority("MEMORY:BaseUrl", "MEMORY_BASE_URL", "http://localhost:5602");

builder.Services.AddHttpClient<ILLMClient, LLMClient>(client => {
    client.BaseAddress = new Uri(llmBaseUrl);
});

builder.Services.AddHttpClient<IMemoryClient, VectorMemoryClient>(client => {
    client.BaseAddress = new Uri(memoryBaseUrl);
});

builder.Services.AddHttpClient<ITextToSpeechClient, TextToSpeechClient>((sp, client) => {
    client.BaseAddress = new Uri("https://supertoneapi.com");
    
    var configuration = sp.GetRequiredService<IConfiguration>();
    var apiKey = configuration.GetValueWithEnvPriority("TTSApiKey", "TTS_API_KEY");
    
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        client.DefaultRequestHeaders.Add("x-sup-api-key", apiKey);
    }
})
.AddTypedClient((httpClient, sp) => {
    var logger = sp.GetRequiredService<ILogger<TextToSpeechClient>>();
    return new TextToSpeechClient(httpClient, logger);
});

// Application Services
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddApplicationServices();
builder.Services.AddScoped<IConversationService, ConversationService>();

builder.Services.AddSingleton<IConnectionRegistry, ConnectionRegistry>();
builder.Services.AddSingleton<IClientConnectionFactory, WebSocketClientConnectionFactory>();
builder.Services.AddSingleton<IMessageBroker, MessageBroker>();

// Infrastructure Repositories 
builder.Services.AddScoped<ICharacterRepository, SqlServerCharacterRepository>();
builder.Services.AddScoped<IConversationRepository, SqlServerConversationRepository>();
builder.Services.AddScoped<IUserRepository, SqlServerUserRepository>();
builder.Services.AddSingleton<ISessionStorage, InMemorySessionStorage>();



// 개발용 서비스 등록
builder.Services.AddSingleton<TestClientLauncher>();

// 개발용 CORS 정책 (모든 origin 허용)
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// 데이터베이스 마이그레이션 자동 적용
using (var scope = app.Services.CreateScope()) {
    var context = scope.ServiceProvider.GetRequiredService<ProjectVGDbContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectVG API V1");
        c.RoutePrefix = "swagger";
    });
}

//app.UseHttpsRedirection();

app.UseGlobalExceptionHandler();

app.UseWebSockets();

// WebSocket 미들웨어를 특정 경로에만 적용
app.UseMiddleware<WebSocketMiddleware>();

app.Use(async (ctx, next) => {
    var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("REQ {method} {path} from {remote}", ctx.Request.Method, ctx.Request.Path, ctx.Connection.RemoteIpAddress);
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

// CORS 미들웨어 적용 (개발 환경에서만)
app.UseCors("AllowAll");

app.MapControllers();

// 개발 환경에서 테스트 클라이언트 자동 실행
if (app.Environment.IsDevelopment())
{
    app.Services.GetRequiredService<TestClientLauncher>().Launch();
}

app.Run();
