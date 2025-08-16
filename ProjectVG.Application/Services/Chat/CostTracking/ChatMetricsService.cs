using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.CostTracking;

namespace ProjectVG.Application.Services.Chat.CostTracking
{
    public class ChatMetricsService : IChatMetricsService
    {
        private readonly ILogger<ChatMetricsService> _logger;
        private readonly AsyncLocal<ChatMetrics?> _currentMetrics = new();

        public ChatMetricsService(ILogger<ChatMetricsService> logger)
        {
            _logger = logger;
        }

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

        public void EndChatMetrics()
        {
            if (_currentMetrics.Value == null) return;

            _currentMetrics.Value.EndTime = DateTime.UtcNow;
            _currentMetrics.Value.TotalDuration = _currentMetrics.Value.EndTime - _currentMetrics.Value.StartTime;
            _currentMetrics.Value.TotalCost = _currentMetrics.Value.ProcessMetrics.Sum(p => p.Cost);
        }

        public ChatMetrics? GetCurrentChatMetrics()
        {
            return _currentMetrics.Value;
        }

        public void LogChatMetrics()
        {
            var metrics = _currentMetrics.Value;
            if (metrics == null) return;

            Console.WriteLine($"[METRICS] 채팅 메트릭 로그 시작: {metrics.SessionId}");

            _logger.LogInformation(
                "채팅 메트릭 - SessionId: {SessionId}, 총 비용: {TotalCost:C}, 총 시간: {TotalDuration}",
                metrics.SessionId, metrics.TotalCost, metrics.TotalDuration);

            foreach (var process in metrics.ProcessMetrics)
            {
                _logger.LogInformation(
                    "  - {ProcessName}: {Duration}ms, 비용: {Cost:C}",
                    process.ProcessName, process.Duration.TotalMilliseconds, process.Cost);
            }
        }
    }
}
