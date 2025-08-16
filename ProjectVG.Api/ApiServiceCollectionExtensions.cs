using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Api.Services;
using ProjectVG.Api.Filters;
using Microsoft.AspNetCore.Authentication.Negotiate;

namespace ProjectVG.Api
{
    public static class ApiServiceCollectionExtensions
    {
        /// <summary>
        /// API 서비스 등록
        /// <summary>
        /// API 관련 서비스(컨트롤러, Swagger, 모델 상태 검증 필터 등)를 IServiceCollection에 등록합니다.
        /// </summary>
        /// <remarks>
        /// 등록 내용:
        /// - MVC 컨트롤러를 추가하고 전역적으로 ModelStateValidationFilter를 등록합니다.
        /// - Endpoints API Explorer 및 Swagger 문서(v1)를 구성합니다.
        /// - TestClientLauncher를 싱글톤으로 등록합니다.
        /// </remarks>
        /// <returns>구성된 IServiceCollection을 반환하여 메서드 체이닝을 지원합니다.</returns>
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
        /// Negotiate 인증 핸들러를 등록하고 기본(폴백) 권한 정책을 비활성화하여 API 인증/인가를 구성합니다.
        /// </summary>
        /// <returns>구성된 IServiceCollection을 반환합니다(메서드 체이닝 지원).</returns>
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
        /// 개발용 CORS 정책
        /// <summary>
        /// 개발 환경에서 사용하기 위한 모든 출처/메서드/헤더 허용 CORS 정책("AllowAll")을 IServiceCollection에 등록합니다.
        /// </summary>
        /// <remarks>
        /// 이 확장 메서드는 개발 및 테스트 용도로 모든 출처와 HTTP 메서드, 헤더를 허용하는 정책을 추가합니다.
        /// 프로덕션 환경에서는 더 제한적인 정책을 사용해야 합니다.
        /// </remarks>
        /// <returns>등록된 IServiceCollection 인스턴스(체이닝 가능).</returns>
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
