using ProjectVG.Infrastructure.ExternalApis.MemoryClient;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Chat.Preprocessors
{
    public class MemoryContextPreprocessor
    {
        private readonly IMemoryClient _memoryClient;
        private readonly ILogger<MemoryContextPreprocessor> _logger;

        public MemoryContextPreprocessor(IMemoryClient memoryClient, ILogger<MemoryContextPreprocessor> logger)
        {
            _memoryClient = memoryClient;
            _logger = logger;
        }

        public async Task<List<string>> CollectMemoryContextAsync(string userMessage)
        {
            try
            {
                var searchResults = await _memoryClient.SearchAsync(userMessage, 3);
                return searchResults.Select(r => r.Text).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "메모리 컨텍스트 수집 실패");
                return new List<string>();
            }
        }
    }
} 