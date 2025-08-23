using Microsoft.AspNetCore.Builder;
using ProjectVG.Api.Middleware;
using ProjectVG.Api.Services;

namespace ProjectVG.Api
{
    public static class ApiMiddlewareExtensions
    {
        /// <summary>
        /// API 미들웨어 파이프라인 구성
        /// </summary>
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

            // WebSocket 지원 (Test 환경 제외)
            if (!environment.IsEnvironment("Test"))
            {
                app.UseWebSockets();
                
                // WebSocket 미들웨어 등록
                app.UseMiddleware<WebSocketMiddleware>();
            }

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
        /// </summary>
        public static IApplicationBuilder UseDevelopmentFeatures(this IApplicationBuilder app)
        {
            // 개발 환경에서 테스트 클라이언트 자동 실행
            var serviceProvider = app.ApplicationServices;
            serviceProvider.GetRequiredService<TestClientLauncher>().Launch();

            return app;
        }
    }
}
