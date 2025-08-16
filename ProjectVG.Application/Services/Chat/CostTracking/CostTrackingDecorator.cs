using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.CostTracking;

namespace ProjectVG.Application.Services.Chat.CostTracking
{
    public class CostTrackingDecorator<T> : ICostTrackingDecorator<T> where T : class
    {
        private readonly T _service;
        private readonly IChatMetricsService _metricsService;
        private readonly string _processName;

        public CostTrackingDecorator(T service, IChatMetricsService metricsService, string processName)
        {
            _service = service;
            _metricsService = metricsService;
            _processName = processName;
        }

        public T Service => _service;

        private decimal ExtractCost(object? result)
        {
            if (result == null) return 0;

            var resultType = result.GetType();
            
            // Cost 속성이 있는 경우
            var costProperty = resultType.GetProperty("Cost");
            if (costProperty != null)
            {
                var costValue = costProperty.GetValue(result);
                if (costValue != null)
                {
                    // double, decimal, int 등 다양한 타입 지원
                    return Convert.ToDecimal(costValue);
                }
            }
            
            return 0;
        }

        public async Task<ChatProcessResult> ProcessAsync(ChatPreprocessContext context)
        {
            _metricsService.StartProcessMetrics(_processName);
            
            try
            {
                // 리플렉션으로 ProcessAsync 메서드 호출
                var method = typeof(T).GetMethod("ProcessAsync", new[] { typeof(ChatPreprocessContext) });
                
                if (method == null)
                    throw new InvalidOperationException($"ProcessAsync 메서드를 찾을 수 없습니다: {typeof(T).Name}");

                var result = await (Task<ChatProcessResult>)method.Invoke(_service, new object[] { context })!;
                
                // Cost 값만 직접 추출
                var cost = ExtractCost(result);
                Console.WriteLine($"[COST_TRACKING] {_processName} - 추출된 비용: {cost:C}");
                _metricsService.EndProcessMetrics(_processName, cost);
                return result;
            }
            catch (Exception ex)
            {
                _metricsService.EndProcessMetrics(_processName, 0, ex.Message);
                throw;
            }
        }

        public async Task ProcessAsync(ChatPreprocessContext context, ChatProcessResult result)
        {
            _metricsService.StartProcessMetrics(_processName);
            
            try
            {
                // 리플렉션으로 ProcessAsync 메서드 호출 (void 반환)
                var method = typeof(T).GetMethod("ProcessAsync", 
                    new[] { typeof(ChatPreprocessContext), typeof(ChatProcessResult) });
                
                if (method == null)
                    throw new InvalidOperationException($"ProcessAsync 메서드를 찾을 수 없습니다: {typeof(T).Name}");

                await (Task)method.Invoke(_service, new object[] { context, result })!;
                
                // Cost 값만 직접 추출
                var cost = ExtractCost(result);
                Console.WriteLine($"[COST_TRACKING] {_processName} - 추출된 비용: {cost:C}");
                _metricsService.EndProcessMetrics(_processName, cost);
            }
            catch (Exception ex)
            {
                _metricsService.EndProcessMetrics(_processName, 0, ex.Message);
                throw;
            }
        }
    }
}
