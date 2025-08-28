using Microsoft.Extensions.DependencyInjection;

namespace ProjectVG.Application.Services.Chat.CostTracking
{
    public static class CostTrackingDecoratorFactory
    {
        /// <summary>
        /// 지정한 서비스 타입 T에 대해 비용 추적 데코레이터를 등록합니다.
        /// </summary>
        /// <remarks>
        /// - 원본 서비스 T를 지정된 ServiceLifetime으로 자체 구현체로 등록합니다.
        /// - ICostTrackingDecorator&lt;T&gt;를 팩토리로 등록하며, 팩토리에서 원본 서비스 T와 IChatMetricsService를 Resolve하여 CostTrackingDecorator&lt;T&gt;를 생성합니다.
        /// - 등록된 서비스들의 의존성이 누락되면 런타임 예외가 발생할 수 있습니다.
        /// </remarks>
        /// <param name="processName">데코레이터가 비용을 집계할 때 사용할 프로세스 식별 이름입니다.</param>
        /// <param name="lifetime">원본 서비스 및 데코레이터에 적용할 ServiceLifetime(기본값: Scoped)입니다.</param>
        /// <returns>데코레이터가 등록된 동일한 IServiceCollection을 반환합니다.</returns>
        public static IServiceCollection AddCostTrackingDecorator<T>(
            this IServiceCollection services,
            string processName,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
            where T : class
        {
            // 원본 서비스 등록
            services.Add(new ServiceDescriptor(typeof(T), typeof(T), lifetime));

            // 비용 추적 데코레이터 등록
            services.Add(new ServiceDescriptor(
                typeof(ICostTrackingDecorator<T>),
                provider =>
                {
                    var service = provider.GetRequiredService<T>();
                    var metricsService = provider.GetRequiredService<IChatMetricsService>();
                    return new CostTrackingDecorator<T>(service, metricsService, processName);
                },
                lifetime));

            return services;
        }
    }
}
