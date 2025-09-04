using ProjectVG.Infrastructure.Integrations.MemoryClient;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Infrastructure.Integrations.MemoryClient.Models;

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

        public async Task<List<string>> CollectMemoryContextAsync(ChatRequestCommand command)
        {
            try {
                var searchQuery = command.UserPrompt;
                var userId = command.UserId.ToString();

                _logger.LogDebug("메모리 검색 쿼리: 원본='{Original}', 의도='{Intent}'",
                    command.UserPrompt, command.UserIntent);

                // 간단한 메모리 타입 선택: 질문이나 회상 관련은 Episodic, 나머지는 Semantic
                var memoryType = ChooseMemoryType(command.UserIntent);
                var searchResults = await _memoryClient.SearchAsync(memoryType, searchQuery, userId, 3);
                return searchResults.Select(r => r.Text).ToList();
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "메모리 컨텍스트 수집 실패");
                return new List<string>();
            }
        }

        private MemoryType ChooseMemoryType(string userIntent)
        {
            // 질문, 회상, 기억 관련 의도는 Episodic 메모리에서 검색
            var episodicKeywords = new[] { "질문", "회상", "기억", "과거", "경험", "언제", "어떻게", "무엇" };
            
            if (episodicKeywords.Any(keyword => userIntent.Contains(keyword)))
            {
                return MemoryType.Episodic;
            }
            
            return MemoryType.Semantic;
        }
    }
}
