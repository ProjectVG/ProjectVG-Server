using Microsoft.AspNetCore.Builder;
using ProjectVG.Api.Middleware;
using ProjectVG.Api.Services;

namespace ProjectVG.Api
{
    public static class ApiMiddlewareExtensions
    {
        /// <summary>
        /// API 미들웨어 파이프라인 구성
        /// <summary>
        /// ASP.NET Core 파이프라인에 API 관련 미들웨어를 등록합니다.
        /// </summary>
        /// <remarks>
        /// 개발 환경인 경우 Swagger 및 Swagger UI를 활성화합니다. 전역 예외 처리, WebSocket 지원 및 WebSocket 미들웨어를 등록하고,
        /// 요청 메서드/경로/원격 IP 로깅 미들웨어를 추가합니다. 이후 인증·인가, CORS("AllowAll"), 라우팅을 설정하고 컨트롤러 엔드포인트를 매핑합니다.
        /// </remarks>
        /// <param name="environment">현재 호스트 환경. 개발 환경일 때 Swagger UI와 엔드포인트를 노출합니다.</param>
        /// <returns>구성된 IApplicationBuilder 인스턴스.</returns>
        public static IApplicationBuilder UseApiMiddleware(this IApplicationBuilder app, IWebHostEnvironment environment)
        {
            // 개발 환경 설정
            if (environment.IsDevelopment()) {
                app.UseSwagger();
                app.UseSwaggerUI(c => {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectVG API V1");
                    c.RoutePrefix = "swagger";
                });
            }

            // 전역 예외 처리
            app.UseGlobalExceptionHandler();

            // WebSocket 지원
            app.UseWebSockets();

            // WebSocket 미들웨어 등록
            app.UseMiddleware<WebSocketMiddleware>();

            // 요청 로깅 미들웨어
            app.Use(async (ctx, next) => {
                var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("REQ {method} {path} from {remote}", ctx.Request.Method, ctx.Request.Path, ctx.Connection.RemoteIpAddress);
                await next();
            });

            // 인증/인가
            app.UseAuthentication();
            app.UseAuthorization();

            // CORS 미들웨어 적용
            app.UseCors("AllowAll");

            // 컨트롤러 매핑
            app.UseRouting();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });

            return app;
        }

        /// <summary>
        /// 개발 환경 전용 기능
        /// <summary>
        /// 애플리케이션 서비스에서 TestClientLauncher를 가져와 개발용 테스트 클라이언트를 실행한 후 동일한 IApplicationBuilder를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 개발 환경에서 테스트 클라이언트를 자동으로 시작하는 데 사용됩니다.
        /// </remarks>
        /// <returns>구성된 IApplicationBuilder 인스턴스 (입력과 동일).</returns>
        /// <exception cref="System.InvalidOperationException">애플리케이션 서비스에 TestClientLauncher가 등록되어 있지 않은 경우 발생합니다.</exception>
        public static IApplicationBuilder UseDevelopmentFeatures(this IApplicationBuilder app)
        {
            // 개발 환경에서 테스트 클라이언트 자동 실행
            var serviceProvider = app.ApplicationServices;
            serviceProvider.GetRequiredService<TestClientLauncher>().Launch();

            return app;
        }
    }
}
