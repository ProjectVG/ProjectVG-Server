using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.CostTracking;

namespace ProjectVG.Application.Services.Chat.CostTracking
{
    public class ChatMetricsService : IChatMetricsService
    {
        private readonly ILogger<ChatMetricsService> _logger;
        private readonly AsyncLocal<ChatMetrics?> _currentMetrics = new();

        /// <summary>
        /// ChatMetricsService의 새 인스턴스를 생성합니다.
        /// </summary>
        public ChatMetricsService(ILogger<ChatMetricsService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 현재 비동기 컨텍스트에 대한 새 채팅 메트릭을 초기화하고 저장합니다.
        /// </summary>
        /// <param name="sessionId">해당 대화의 고유 세션 식별자.</param>
        /// <param name="userId">대화를 시작한 사용자 식별자.</param>
        /// <param name="characterId">대화에 사용된 캐릭터 또는 에이전트 식별자.</param>
        public void StartChatMetrics(string sessionId, string userId, string characterId)
        {
            _currentMetrics.Value = new ChatMetrics
            {
                SessionId = sessionId,
                UserId = userId,
                CharacterId = characterId,
                StartTime = DateTime.UtcNow
            };
            Console.WriteLine($"[METRICS] 채팅 메트릭 시작: {sessionId}");
        }

        /// <summary>
        /// 현재 비동기 컨텍스트의 채팅 메트릭에 새로운 프로세스 항목을 생성하여 시작 시간을 기록합니다.
        /// </summary>
        /// <param name="processName">측정할 프로세스의 식별명(예: "Tokenization", "ModelCall" 등). 현재 컨텍스트에 활성 ChatMetrics가 없으면 호출 시 아무 작업도 수행하지 않습니다.</param>
        public void StartProcessMetrics(string processName)
        {
            if (_currentMetrics.Value == null) return;

            var processMetrics = new ProcessMetrics
            {
                ProcessName = processName,
                StartTime = DateTime.UtcNow
            };

            _currentMetrics.Value.ProcessMetrics.Add(processMetrics);
            Console.WriteLine($"[METRICS] 프로세스 시작: {processName}");
        }

        /// <summary>
        /// 현재 컨텍스트의 활성 채팅 메트릭에서 지정된 이름의 미종료 프로세스를 종료하고 관련 속성(종료시간, 지속시간, 비용, 오류, 추가 데이터)을 설정합니다.
        /// </summary>
        /// <param name="processName">종료할 프로세스의 이름(식별자).</param>
        /// <param name="cost">해당 프로세스에 기록할 비용(기본값 0).</param>
        /// <param name="errorMessage">프로세스 종료 시 기록할 오류 메시지(있을 경우).</param>
        /// <param name="additionalData">프로세스에 연관된 추가 메타데이터(있을 경우).</param>
        /// <remarks>
        /// 현재 컨텍스트에 활성 ChatMetrics가 없거나 해당 이름의 미종료 프로세스를 찾지 못하면 아무 작업도 수행하지 않고 반환합니다.
        /// 종료 시점은 UTC 기준 현재 시간으로 설정되며, Duration은 StartTime으로부터의 차이로 계산됩니다.
        /// </remarks>
        public void EndProcessMetrics(string processName, decimal cost = 0, string? errorMessage = null, Dictionary<string, object>? additionalData = null)
        {
            if (_currentMetrics.Value == null) return;

            var processMetrics = _currentMetrics.Value.ProcessMetrics
                .FirstOrDefault(p => p.ProcessName == processName && p.EndTime == default);

            if (processMetrics != null)
            {
                processMetrics.EndTime = DateTime.UtcNow;
                processMetrics.Duration = processMetrics.EndTime - processMetrics.StartTime;
                processMetrics.Cost = cost;
                processMetrics.ErrorMessage = errorMessage;
                processMetrics.AdditionalData = additionalData;
            }
        }

        /// <summary>
        /// 현재 비동기 컨텍스트의 채팅 메트릭을 종료하고 요약 값을 계산합니다.
        /// </summary>
        /// <remarks>
        /// 활성 메트릭이 없으면 아무 작업도 수행하지 않습니다. 종료 시점(UTC)을 기록하고 전체 지속 시간과
        /// 모든 프로세스의 비용 합계를 TotalDuration, TotalCost에 각각 저장합니다.
        /// </remarks>
        public void EndChatMetrics()
        {
            if (_currentMetrics.Value == null) return;

            _currentMetrics.Value.EndTime = DateTime.UtcNow;
            _currentMetrics.Value.TotalDuration = _currentMetrics.Value.EndTime - _currentMetrics.Value.StartTime;
            _currentMetrics.Value.TotalCost = _currentMetrics.Value.ProcessMetrics.Sum(p => p.Cost);
        }

        /// <summary>
        /// 현재 비동기 컨텍스트에 연결된 ChatMetrics 인스턴스를 반환합니다.
        /// </summary>
        /// <returns>현재 컨텍스트의 ChatMetrics 객체 또는 존재하지 않으면 null.</returns>
        public ChatMetrics? GetCurrentChatMetrics()
        {
            return _currentMetrics.Value;
        }

        /// <summary>
        /// 현재 비동기 컨텍스트에 저장된 채팅 메트릭을 조회하여 콘솔과 로거에 요약 및 프로세스별 세부 비용/시간을 기록합니다.
        /// </summary>
        /// <remarks>
        /// 메트릭이 없으면 아무 동작도 하지 않습니다.
        /// 총비용과 프로세스별 비용은 내부 단위(정수형 비용)를 달러 단위로 변환하기 위해 100_000.0으로 나누어 로그에 표시합니다.
        /// 출력 형식은 총비용(소수점 6자리)과 각 프로세스의 지속시간(밀리초) 및 비용(달러)입니다.
        /// </remarks>
        public void LogChatMetrics()
        {
            var metrics = _currentMetrics.Value;
            if (metrics == null) return;

            Console.WriteLine($"[METRICS] 채팅 메트릭 로그 시작: {metrics.SessionId}");

            var totalCostInDollars = (double)metrics.TotalCost / 100_000.0;
            _logger.LogInformation(
                "채팅 메트릭 - UserId: {UserId}, 총 비용: ${TotalCost:F6}, 총 시간: {TotalDuration}",
                metrics.SessionId, totalCostInDollars, metrics.TotalDuration);

            foreach (var process in metrics.ProcessMetrics)
            {
                var processCostInDollars = (double)process.Cost / 100_000.0;
                _logger.LogInformation(
                    "  - {ProcessName}: {Duration}ms, 비용: ${Cost:F6}",
                    process.ProcessName, process.Duration.TotalMilliseconds, processCostInDollars);
            }
        }
    }
}
