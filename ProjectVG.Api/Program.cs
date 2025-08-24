using ProjectVG.Application;
using ProjectVG.Api.Configuration;
using ProjectVG.Api;
using ProjectVG.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 환경 변수 치환 활성화 (${ENV_VAR} 문법 지원)
builder.Configuration.AddEnvironmentVariableSubstitution(builder.Configuration);

// 서버 설정
var port = builder.Configuration.GetValue<int>("Port", 7900);
builder.WebHost.ConfigureKestrel(options => {
    options.ListenAnyIP(port);
});

// 모듈별 서비스 등록
builder.Services.AddApiServices();
builder.Services.AddApiAuthentication();

// OAuth2 활성화 여부 확인 (환경 변수 지원)
var oauth2Enabled = builder.Configuration.GetValue<bool>("OAuth2:Enabled", true);
if (oauth2Enabled)
{
    builder.Services.AddOAuth2Authentication();
}

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddDevelopmentCors();

var app = builder.Build();

// 데이터베이스 마이그레이션 자동 적용
app.Services.MigrateDatabase();

// 미들웨어 파이프라인 구성
app.UseApiMiddleware(app.Environment);

// 개발 환경 전용 기능
if (app.Environment.IsDevelopment())
{
    app.UseDevelopmentFeatures();
}

app.Run();
