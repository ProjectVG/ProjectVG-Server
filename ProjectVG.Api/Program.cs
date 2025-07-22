using ProjectVG.Application.Middlewares;
using Microsoft.AspNetCore.Authentication.Negotiate;
using ProjectVG.Infrastructure.ExternalApis.LLMClient;
using ProjectVG.Infrastructure.ExternalApis.MemoryClient;
using ProjectVG.Application.Services.LLM;
using ProjectVG.Application.Services.Chat;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Session;
using ProjectVG.Application.Services.Voice;
using ProjectVG.Application.Services.User;
using ProjectVG.Infrastructure.Repositories;
using ProjectVG.Infrastructure.Repositories.InMemory;
using ProjectVG.Infrastructure.Repositories.SqlServer;
using ProjectVG.Infrastructure.ExternalApis.TextToSpeech;
using ProjectVG.Application.Services.Chat.Extensions;
using ProjectVG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ProjectVG API",
        Version = "v1",
        Description = "ProjectVG API Server"
    });
});

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

// [Authorize]가 붙은 엔드포인트만 인증을 요구하도록 설정
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null;
});

// Entity Framework Core 설정
builder.Services.AddDbContext<ProjectVGDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// External API Clients
builder.Services.AddHttpClient<ILLMClient, LLMClient>(client => {
    client.BaseAddress = new Uri("http://localhost:5601");
});

builder.Services.AddHttpClient<IMemoryClient, VectorMemoryClient>(client => {
    client.BaseAddress = new Uri("http://localhost:5602");
});

builder.Services.AddHttpClient<ITextToSpeechClient, TextToSpeechClient>((sp, client) => {
    client.BaseAddress = new Uri("https://supertoneapi.com");
})
.AddTypedClient((httpClient, sp) => {
    var logger = sp.GetRequiredService<ILogger<TextToSpeechClient>>();
    return new TextToSpeechClient(httpClient, logger);
});


// Application Services
builder.Services.AddScoped<ILLMService, ChatLLMService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IVoiceService, VoiceService>();
builder.Services.AddScoped<IUserService, UserService>();

// Infrastructure Repositories 
builder.Services.AddScoped<ICharacterRepository, SqlServerCharacterRepository>();
builder.Services.AddScoped<IConversationRepository, SqlServerConversationRepository>();
builder.Services.AddScoped<IUserRepository, SqlServerUserRepository>();
builder.Services.AddSingleton<ISessionRepository, InMemorySessionRepository>();

// DI 확장 메서드 등록
builder.Services.AddChatOrchestrationServices();

var app = builder.Build();

// 데이터베이스 마이그레이션 자동 적용
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProjectVGDbContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectVG API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseWebSockets();

// WebSocket 미들웨어를 특정 경로에만 적용
app.UseMiddleware<WebSocketMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// WebSocket 전용 포트 추가
app.Urls.Add("http://localhost:5287");

app.Run(); 