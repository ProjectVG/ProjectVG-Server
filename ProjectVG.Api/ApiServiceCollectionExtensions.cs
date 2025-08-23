using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Api.Services;
using ProjectVG.Api.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Infrastructure.Auth.Models;

namespace ProjectVG.Api
{
    public static class ApiServiceCollectionExtensions
    {
        /// <summary>
        /// API 서비스 등록
        /// </summary>
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            services.AddControllers(options => {
                options.Filters.Add<ModelStateValidationFilter>();
            });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo {
                    Title = "ProjectVG API",
                    Version = "v1",
                    Description = "ProjectVG API Server"
                });
            });

            services.AddSingleton<TestClientLauncher>();

            return services;
        }

        /// <summary>
        /// JWT Bearer 인증 및 인가 서비스
        /// </summary>
        public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            
            services.AddScoped<IKeyStore, FileSystemKeyStore>();
            services.AddHostedService<JwtKeyProviderService>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // 토큰 검증 매개변수는 startup 시 초기화하고, 런타임에 동적으로 키를 로드
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                // 토큰 검증 시 동적으로 키 로드
                options.Events.OnTokenValidated = async context =>
                {
                    var keyStore = context.HttpContext.RequestServices.GetRequiredService<IKeyStore>();
                    var validationKeys = await keyStore.GetValidationKeysAsync();
                    context.Options.TokenValidationParameters.IssuerSigningKeys = validationKeys;
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                        {
                            context.Token = accessToken;
                        }
                        
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAuthentication", policy => 
                    policy.RequireAuthenticatedUser());
                
                options.AddPolicy("RequireUser", policy => 
                    policy.RequireAuthenticatedUser()
                          .RequireClaim("sub"));
                          
                options.DefaultPolicy = options.GetPolicy("RequireAuthentication")!;
            });

            return services;
        }

        /// <summary>
        /// 개발용 CORS 정책
        /// </summary>
        public static IServiceCollection AddDevelopmentCors(this IServiceCollection services)
        {
            services.AddCors(options => {
                options.AddPolicy("AllowAll",
                    policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });

            return services;
        }
    }
}
