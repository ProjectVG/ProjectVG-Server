using System.Text;
using System.Text.Json;
using MainAPI_Server.Models.External.LLM;

namespace MainAPI_Server.Clients.LLM
{
    public class LLMClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LLMClient> _logger;

        public LLMClient(HttpClient httpClient, ILogger<LLMClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }


        public async Task<LLMResponse> GenerateResponseAsync(string userMessage, List<string> conversationContext, List<string> memoryContext)
        {
            LLMResponse response = new LLMResponse();

            return response;
        }
    }
} 