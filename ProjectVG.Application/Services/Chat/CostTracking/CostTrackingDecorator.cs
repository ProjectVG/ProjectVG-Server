using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.CostTracking;
using ProjectVG.Domain.Entities.ConversationHistorys;

namespace ProjectVG.Application.Services.Chat.CostTracking
{
    public class CostTrackingDecorator<T> : ICostTrackingDecorator<T> where T : class
    {
        private readonly T _service;
        private readonly IChatMetricsService _metricsService;
        private readonly string _processName;

        /// <summary>
        /// 지정된 서비스 인스턴스를 감싸고, 주어진 메트릭 서비스로 비용 추적을 수행하는 데코레이터를 초기화합니다.
        /// </summary>
        /// <param name="processName">메트릭에서 사용될 프로세스 식별자(이름). End/Start 호출에서 동일하게 사용됩니다.</param>
        public CostTrackingDecorator(T service, IChatMetricsService metricsService, string processName)
        {
            _service = service;
            _metricsService = metricsService;
            _processName = processName;
        }

        public T Service => _service;

        /// <summary>
        /// 주어진 객체에서 "Cost" 프로퍼티를 찾아 숫자값을 소수(decimal)로 반환합니다.
        /// </summary>
        /// <param name="result">Cost 프로퍼티를 검색할 대상 객체(널 허용).</param>
        /// <returns>Cost 프로퍼티가 존재하고 값이 있으면 해당 값을 decimal로 변환한 값, 그렇지 않으면 0.</returns>
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

        /// <summary>
        /// 지정된 ChatProcessContext를 사용해 내부 서비스의 ProcessAsync를 호출하고 해당 실행의 비용을 추적하여 메트릭을 기록합니다.
        /// </summary>
        /// <param name="context">내부 서비스에 전달할 처리 컨텍스트. 이 컨텍스트에 있는 `Cost` 속성(있을 경우)을 읽어 비용으로 사용합니다.</param>
        /// <exception cref="InvalidOperationException">
        /// 내부 서비스에서 ChatProcessContext를 인수로 받는 `ProcessAsync` 메서드를 찾을 수 없거나,
        /// 해당 메서드 호출 결과가 null이거나 Task가 아닌 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// - 메서드 시작 시 StartProcessMetrics를 호출하고, 완료 시 ExtractCost로 추출한 비용을 EndProcessMetrics에 전달합니다.
        /// - 내부 호출에서 발생한 예외는 EndProcessMetrics에 0과 예외 메시지를 전달한 뒤 그대로 다시 throw됩니다.
        /// </remarks>
        public async Task ProcessAsync(ChatProcessContext context)
        {
            _metricsService.StartProcessMetrics(_processName);
            
            try
            {
                // 리플렉션으로 ProcessAsync 메서드 호출
                var method = typeof(T).GetMethod("ProcessAsync", new[] { typeof(ChatProcessContext) });
                
                if (method == null)
                    throw new InvalidOperationException($"ProcessAsync 메서드를 찾을 수 없습니다: {typeof(T).Name}");

                var invokeResult = method.Invoke(_service, new object[] { context });
                if (invokeResult == null)
                    throw new InvalidOperationException($"ProcessAsync 메서드 호출 결과가 null입니다: {typeof(T).Name}");
                
                if (invokeResult is not Task taskResult)
                    throw new InvalidOperationException($"ProcessAsync 메서드 반환 타입이 올바르지 않습니다: {typeof(T).Name}");
                
                await taskResult;
                
                // Cost 값만 직접 추출
                var cost = ExtractCost(context);
                Console.WriteLine($"[COST_TRACKING] {_processName} - 추출된 비용: {cost:F0} Cost");
                Console.WriteLine($"[COST_TRACKING] {_processName} - 컨텍스트 타입: {context?.GetType().Name}, Cost 속성 값: {context?.GetType().GetProperty("Cost")?.GetValue(context)}");
                _metricsService.EndProcessMetrics(_processName, cost);
            }
            catch (Exception ex)
            {
                _metricsService.EndProcessMetrics(_processName, 0, ex.Message);
                throw;
            }
        }



        /// <summary>
        /// 주어진 사용자 입력과 대화 기록을 내부 서비스의 `ProcessAsync(string, IEnumerable{ConversationHistory})`에 위임하여 처리하고,
        /// 호출 결과에서 `Cost` 값을 추출해 메트릭을 기록한 뒤 결과를 반환합니다.
        /// </summary>
        /// <param name="userInput">분석할 사용자 입력 문자열.</param>
        /// <param name="conversationHistory">분석에 참조할 대화 히스토리 컬렉션.</param>
        /// <returns>내부 서비스가 반환한 UserInputAnalysis 인스턴스.</returns>
        /// <exception cref="InvalidOperationException">
        /// 내부 서비스에 적합한 `ProcessAsync(string, IEnumerable{ConversationHistory})` 메서드가 없거나,
        /// 해당 메서드 호출 결과가 null이거나 기대한 반환 타입(Task&lt;UserInputAnalysis&gt;)이 아닌 경우 발생합니다.
        /// </exception>
        public async Task<UserInputAnalysis> ProcessAsync(string userInput, IEnumerable<ConversationHistory> conversationHistory)
        {
            _metricsService.StartProcessMetrics(_processName);
            
            try
            {
                // 리플렉션으로 ProcessAsync 메서드 호출
                var method = typeof(T).GetMethod("ProcessAsync", new[] { typeof(string), typeof(IEnumerable<ConversationHistory>) });
                
                if (method == null)
                    throw new InvalidOperationException($"ProcessAsync 메서드를 찾을 수 없습니다: {typeof(T).Name}");

                var invokeResult = method.Invoke(_service, new object[] { userInput, conversationHistory });
                if (invokeResult == null)
                    throw new InvalidOperationException($"ProcessAsync 메서드 호출 결과가 null입니다: {typeof(T).Name}");
                
                if (invokeResult is not Task<UserInputAnalysis> taskResult)
                    throw new InvalidOperationException($"ProcessAsync 메서드 반환 타입이 올바르지 않습니다: {typeof(T).Name}");
                
                var result = await taskResult!;
                
                // Cost 값만 직접 추출
                var cost = ExtractCost(result);
                Console.WriteLine($"[COST_TRACKING] {_processName} - 추출된 비용: {cost:F0} Cost");
                Console.WriteLine($"[COST_TRACKING] {_processName} - 원본 결과 타입: {result?.GetType().Name}, Cost 속성 값: {result?.GetType().GetProperty("Cost")?.GetValue(result)}");
                _metricsService.EndProcessMetrics(_processName, cost);
                return result;
            }
            catch (Exception ex)
            {
                _metricsService.EndProcessMetrics(_processName, 0, ex.Message);
                throw;
            }
        }
    }
}
