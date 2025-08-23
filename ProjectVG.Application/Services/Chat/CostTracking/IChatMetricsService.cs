using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat.CostTracking
{
    public interface IChatMetricsService
    {
        void StartChatMetrics(string sessionId, string userId, string characterId);
        void StartProcessMetrics(string processName);
        void EndProcessMetrics(string processName, decimal cost = 0, string? errorMessage = null, Dictionary<string, object>? additionalData = null);
        void EndChatMetrics();
        ChatMetrics? GetCurrentChatMetrics();
        void LogChatMetrics();
    }
}
