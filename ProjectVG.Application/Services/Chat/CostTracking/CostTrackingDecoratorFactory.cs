using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.CostTracking;

namespace ProjectVG.Application.Services.Chat.CostTracking
{
    public static class CostTrackingDecoratorFactory
    {
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
