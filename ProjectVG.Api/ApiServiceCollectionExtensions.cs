using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Api.Services;
using ProjectVG.Api.Filters;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;

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
        /// 인증 및 인가 서비스
        /// </summary>
        public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();

            services.AddAuthorization(options => {
                options.FallbackPolicy = null;
            });

            return services;
        }

        /// <summary>
        /// OAuth2 인증 서비스 (선택적)
        /// </summary>
        public static IServiceCollection AddOAuth2Authentication(this IServiceCollection services)
        {
            // OAuth2는 별도 컨트롤러에서 처리하므로 기본 인증만 설정
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie();

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
