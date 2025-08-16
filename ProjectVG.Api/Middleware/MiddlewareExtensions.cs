namespace ProjectVG.Api.Middleware
{
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// 전역 예외 처리 미들웨어인 <see cref="GlobalExceptionHandler"/>를 요청 파이프라인에 등록합니다.
        /// </summary>
        /// <param name="builder">미들웨어를 등록할 ASP.NET Core 애플리케이션 빌더.</param>
        /// <returns>미들웨어 등록 후 동일한 <see cref="IApplicationBuilder"/> 인스턴스(체이닝 가능).</returns>
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandler>();
        }
    }
}
