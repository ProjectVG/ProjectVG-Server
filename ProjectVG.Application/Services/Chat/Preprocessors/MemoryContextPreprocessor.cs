using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Infrastructure.Integrations.MemoryClient.Models;
using Microsoft.IdentityModel.Tokens;

namespace ProjectVG.Application.Services.Chat.Preprocessors
{
    public class MemoryContextPreprocessor
    {
        private readonly IMemoryClient _memoryClient;
        private readonly ILogger<MemoryContextPreprocessor> _logger;

        public MemoryContextPreprocessor(
            IMemoryClient memoryClient,
            ILogger<MemoryContextPreprocessor> logger)
        {
            _memoryClient = memoryClient;
            _logger = logger;
        }

        public async Task<List<string>> CollectMemoryContextAsync(string userId, string userMessage, UserInputAnalysis analysis)
        {
            try {
                var searchQuery = DetermineSearchQuery(userMessage, analysis);

                _logger.LogDebug("메모리 검색 쿼리: 원본='{Original}', 향상된='{Enhanced}', 키워드={Keywords}",
                    userMessage, searchQuery, string.Join(",", analysis.Keywords));

                var memoryType = ChooseMemoryType(analysis);
                var searchResults = await _memoryClient.SearchAsync(memoryType, searchQuery, userId, 3);
                return searchResults.Select(r => r.Text).ToList();
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "메모리 컨텍스트 수집 실패");
                return new List<string>();
            }
        }

        private string DetermineSearchQuery(string originalMessage, UserInputAnalysis analysis)
        {
            if (!string.IsNullOrWhiteSpace(analysis.EnhancedQuery)) {
                return analysis.EnhancedQuery;
            }

            if (analysis.Keywords?.Any() == true) {
                return string.Join(" ", analysis.Keywords);
            }

            return originalMessage;
        }

        private MemoryType ChooseMemoryType(UserInputAnalysis analysis)
        {
            if (analysis.Emotions?.Any() == true || analysis.ContainsTemporalExpression.Equals(String.Empty)) {
                return MemoryType.Episodic;
            }
            return MemoryType.Semantic;
        }
    }
}
