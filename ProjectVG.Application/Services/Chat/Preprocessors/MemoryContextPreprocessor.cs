using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat.Preprocessors
{
    public class MemoryContextPreprocessor
    {
        private readonly IMemoryClient _memoryClient;
        private readonly ILogger<MemoryContextPreprocessor> _logger;

        /// <summary>
        /// MemoryContextPreprocessor의 새 인스턴스를 초기화합니다.
        /// </summary>
        public MemoryContextPreprocessor(
            IMemoryClient memoryClient,
            ILogger<MemoryContextPreprocessor> logger)
        {
            _memoryClient = memoryClient;
            _logger = logger;
        }

        /// <summary>
        /// 사용자 입력 및 분석 결과를 바탕으로 메모리 저장소를 검색하여 관련 텍스트 항목들을 수집합니다.
        /// </summary>
        /// <param name="collection">검색할 메모리 컬렉션의 이름.</param>
        /// <param name="userMessage">원본 사용자 메시지(검색을 위한 기본 입력).</param>
        /// <param name="analysis">사용자 입력에 대한 분석 결과(향상된 쿼리 또는 키워드 포함).</param>
        /// <returns>검색된 메모리 항목들의 텍스트 목록을 담은 Task. 오류 발생 시 빈 목록을 반환합니다.</returns>
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

        /// <summary>
        /// 사용자 입력 분석 결과를 바탕으로 메모리 검색에 사용할 쿼리를 결정한다.
        /// </summary>
        /// <param name="originalMessage">원본 사용자 메시지(최후의 대체값).</param>
        /// <param name="analysis">분석 결과; <see cref="UserInputAnalysis.EnhancedQuery"/>가 있으면 우선 사용하고, 없으면 <see cref="UserInputAnalysis.Keywords"/>를 공백으로 연결한 값을 사용한다.</param>
        /// <returns>
        /// 검색에 사용할 쿼리 문자열.
        /// 우선순위: 1) analysis.EnhancedQuery(공백/널이 아닌 경우) 2) analysis.Keywords를 공백으로 연결한 문자열(키워드가 존재하는 경우) 3) originalMessage.
        /// </returns>
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
