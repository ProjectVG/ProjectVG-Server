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
            var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
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

            // WebSocket 지원 - 구성 가능한 옵션 사용
            var webSocketOptions = GetWebSocketOptions(configuration);
            app.UseWebSockets(webSocketOptions);

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
        /// </summary>
        public static IApplicationBuilder UseDevelopmentFeatures(this IApplicationBuilder app)
        {
            // 개발 환경에서 테스트 클라이언트 자동 실행
            var serviceProvider = app.ApplicationServices;
            serviceProvider.GetRequiredService<TestClientLauncher>().Launch();

            return app;
        }

        /// <summary>
        /// WebSocket 옵션을 구성 파일과 환경 변수에서 가져옵니다
        /// </summary>
        private static WebSocketOptions GetWebSocketOptions(IConfiguration configuration)
        {
            var options = new WebSocketOptions();

            // KeepAliveInterval 설정 (환경 변수 > appsettings.json 순서)
            var keepAliveMinutes = Environment.GetEnvironmentVariable("WEBSOCKET_KEEPALIVE_MINUTES");
            if (string.IsNullOrEmpty(keepAliveMinutes))
            {
                keepAliveMinutes = configuration.GetValue<string>("WebSocket:KeepAliveIntervalMinutes");
            }

            if (double.TryParse(keepAliveMinutes, out var minutes))
            {
                if (minutes <= 0)
                {
                    options.KeepAliveInterval = TimeSpan.Zero; // KeepAlive 비활성화
                }
                else
                {
                    options.KeepAliveInterval = TimeSpan.FromMinutes(minutes);
                }
            }
            else
            {
                // 기본값: KeepAlive 비활성화 (연결 안정성을 위해)
                options.KeepAliveInterval = TimeSpan.Zero;
            }

            // 수신 버퍼 크기 설정
            var receiveBufferSize = Environment.GetEnvironmentVariable("WEBSOCKET_RECEIVE_BUFFER_SIZE") ??
                                  configuration.GetValue<string>("WebSocket:ReceiveBufferSize");
            if (int.TryParse(receiveBufferSize, out var recvSize) && recvSize > 0)
            {
                options.ReceiveBufferSize = recvSize;
            }

            // 송신 버퍼 크기 설정 (WebSocketOptions에는 없으므로 로깅만)
            var sendBufferSize = Environment.GetEnvironmentVariable("WEBSOCKET_SEND_BUFFER_SIZE") ??
                               configuration.GetValue<string>("WebSocket:SendBufferSize");

            // 콘솔 로깅으로 설정 확인
            Console.WriteLine($"[WebSocket 설정] KeepAlive: {(options.KeepAliveInterval == TimeSpan.Zero ? "비활성화" : $"{options.KeepAliveInterval.TotalMinutes}분")}, " +
                            $"ReceiveBuffer: {options.ReceiveBufferSize} bytes" +
                            $"{(int.TryParse(sendBufferSize, out var sendSize) && sendSize > 0 ? $", SendBuffer: {sendSize} bytes (참고용)" : "")}");

            return options;
        }
    }
}
