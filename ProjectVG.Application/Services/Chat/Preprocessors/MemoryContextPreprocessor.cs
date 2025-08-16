using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using ProjectVG.Application.Models.Chat;

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

        public async Task<List<string>> CollectMemoryContextAsync(string collection, string userMessage, UserInputAnalysis analysis)
        {
            try {
                // 분석 결과를 기반으로 검색 쿼리 결정
                var searchQuery = DetermineSearchQuery(userMessage, analysis);

                _logger.LogDebug("메모리 검색 쿼리: 원본='{Original}', 향상된='{Enhanced}', 키워드={Keywords}",
                    userMessage, searchQuery, string.Join(",", analysis.Keywords));

                // 향상된 쿼리로 메모리 검색
                var searchResults = await _memoryClient.SearchAsync(collection, searchQuery, 3);
                return searchResults.Select(r => r.Text).ToList();
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "메모리 컨텍스트 수집 실패");
                return new List<string>();
            }
        }

        private string DetermineSearchQuery(string originalMessage, UserInputAnalysis analysis)
        {
            // 향상된 쿼리가 있으면 사용, 없으면 원본 메시지 사용
            if (!string.IsNullOrWhiteSpace(analysis.EnhancedQuery)) {
                return analysis.EnhancedQuery;
            }

            // 키워드가 있으면 키워드 기반으로 쿼리 구성
            if (analysis.Keywords?.Any() == true) {
                return string.Join(" ", analysis.Keywords);
            }

            // 기본값: 원본 메시지
            return originalMessage;
        }
    }
}
