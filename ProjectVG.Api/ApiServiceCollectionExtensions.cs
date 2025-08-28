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
        /// <summary>
        /// Negotiate(Windows) 인증 스킴을 추가하고 전역 대체 권한 정책(FallbackPolicy)을 제거하여 애플리케이션의 인증/인가를 구성합니다.
        /// </summary>
        /// <returns>구성된 IServiceCollection 인스턴스.</returns>
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
        /// <summary>
        /// 쿠키 인증을 기본 인증 방식으로 설정하여 OAuth2 흐름을 별도 컨트롤러에서 처리할 수 있도록 구성합니다.
        /// </summary>
        /// <returns>구성된 IServiceCollection을 반환합니다.</returns>
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
        /// <summary>
        /// 개발 환경에서 사용하도록 모든 출처, 모든 HTTP 메서드 및 모든 헤더를 허용하고
        /// "X-Access-Token", "X-Refresh-Token", "X-Expires-In", "X-UID" 응답 헤더를 노출하는
        /// "AllowAll" CORS 정책을 DI 컨테이너에 등록합니다.
        /// </summary>
        /// <returns>구성된 IServiceCollection을 반환합니다.</returns>
        public static IServiceCollection AddDevelopmentCors(this IServiceCollection services)
        {
            services.AddCors(options => {
                options.AddPolicy("AllowAll",
                    policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
                        .WithExposedHeaders("X-Access-Token", "X-Refresh-Token", "X-Expires-In", "X-UID"));
            });

            return services;
        }
    }
}
