using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.MemoryClient;

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

        public async Task<List<string>> CollectMemoryContextAsync(string collection, string userMessage)
        {
            try
            {
                var searchResults = await _memoryClient.SearchAsync(collection, userMessage, 3);
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