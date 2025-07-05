using MainAPI_Server.Middlewares;
using Microsoft.AspNetCore.Authentication.Negotiate;

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
    client.BaseAddress = new Uri("http://localhost:5001");
});

builder.Services.AddHttpClient<MainAPI_Server.Clients.LLM.ILLMClient, MainAPI_Server.Clients.LLM.LLMClient>(client => {
    client.BaseAddress = new Uri("http://localhost:5002");
});

builder.Services.AddScoped<MainAPI_Server.Services.Chat.IChatService, MainAPI_Server.Services.Chat.ChatService>();
builder.Services.AddScoped<MainAPI_Server.Services.Chat.IConversationService, MainAPI_Server.Services.Chat.ConversationService>();

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
