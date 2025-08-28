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

        /// <summary>
        — 사용자 입력 분석을 기반으로 메모리에서 관련 컨텍스트 텍스트를 비동기로 수집합니다.
        /// </summary>
        /// <param name="collection">검색할 메모리 컬렉션 이름.</param>
        /// <param name="userMessage">원본 사용자 메시지(향상된 쿼리나 키워드가 없을 때 대체로 사용됨).</param>
        /// <param name="analysis">향상된 쿼리, 키워드, 감정, 시간 표현 등 검색 쿼리와 메모리 유형을 결정하기 위한 분석 결과.</param>
        /// <returns>
        /// 검색된 메모리 항목들의 텍스트 목록을 반환합니다. 검색 실패나 예외 발생 시 빈 목록을 반환합니다.
        /// </returns>
        public async Task<List<string>> CollectMemoryContextAsync(string collection, string userMessage, UserInputAnalysis analysis)
        {
            try {
                var searchQuery = DetermineSearchQuery(userMessage, analysis);

                _logger.LogDebug("메모리 검색 쿼리: 원본='{Original}', 향상된='{Enhanced}', 키워드={Keywords}",
                    userMessage, searchQuery, string.Join(",", analysis.Keywords));

                var memoryType = ChooseMemoryType(analysis);
                var searchResults = await _memoryClient.SearchAsync(memoryType, searchQuery, collection, 3);
                return searchResults.Select(r => r.Text).ToList();
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "메모리 컨텍스트 수집 실패");
                return new List<string>();
            }
        }

        /// <summary>
        /// 검색에 사용할 쿼리를 결정합니다.
        /// </summary>
        /// <remarks>
        /// 우선적으로 UserInputAnalysis.EnhancedQuery가 비어있지 않으면 이를 반환하고, 그렇지 않으면 분석된 키워드들이 존재하면 키워드를 공백으로 결합한 문자열을 반환합니다. 둘 다 없으면 원본 사용자 메시지를 그대로 쿼리로 사용합니다.
        /// </remarks>
        /// <param name="originalMessage">사용자 원문 메시지(모든 다른 소스가 없을 때의 최종 폴백).</param>
        /// <param name="analysis">사용자 입력에 대한 분석 결과(EnhancedQuery 또는 Keywords를 검사).</param>
        /// <returns>검색에 사용할 쿼리 문자열.</returns>
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

        /// <summary>
        /// 주어진 사용자 입력 분석에 따라 검색에 사용할 메모리 유형을 결정합니다.
        /// </summary>
        /// <param name="analysis">사용자 입력 분석 결과(감정 목록 및 시간 표현 포함).</param>
        /// <returns>
        /// analysis.Emotions가 비어있지 않거나 analysis.ContainsTemporalExpression가 빈 문자열인 경우 <see cref="MemoryType.Episodic"/>을 반환하고,
        /// 그렇지 않으면 <see cref="MemoryType.Semantic"/>을 반환합니다.
        /// </returns>
        private MemoryType ChooseMemoryType(UserInputAnalysis analysis)
        {
            if (analysis.Emotions?.Any() == true || analysis.ContainsTemporalExpression.Equals(String.Empty)) {
                return MemoryType.Episodic;
            }
            return MemoryType.Semantic;
        }
    }
}
