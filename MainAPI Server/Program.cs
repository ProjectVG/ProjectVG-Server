using MainAPI_Server.Middlewares;
using Microsoft.AspNetCore.Authentication.Negotiate;
using MainAPI_Server.Clients.LLM;
using MainAPI_Server.Clients.Memory;
using MainAPI_Server.Services.Conversation;
using MainAPI_Server.Services.Chat;
using MainAPI_Server.Services.Session;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddHttpClient<IMemoryStoreClient, VectorMemoryClient>(client => {
    client.BaseAddress = new Uri("http://localhost:5602");
});

builder.Services.AddHttpClient<ILLMClient, LLMClient>(client => {
    client.BaseAddress = new Uri("http://localhost:5601");
});

builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<IConversationService, ConversationService>();
builder.Services.AddSingleton<ISessionManager, SessionManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseWebSockets();
app.UseMiddleware<WebSocketMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
