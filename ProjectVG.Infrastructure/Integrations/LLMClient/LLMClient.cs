using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.LLMClient.Models;
using Microsoft.Extensions.Configuration;

namespace ProjectVG.Infrastructure.Integrations.LLMClient
{
    public class LLMClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LLMClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public LLMClient(HttpClient httpClient, ILogger<LLMClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            _jsonOptions = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            _httpClient.BaseAddress = new Uri(configuration["LLM:BaseUrl"] ?? "");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }
        public async Task<LLMResponse> SendRequestAsync(LLMRequest request)
        {
            try {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("LLM 요청 시작: {Model}", request.Model);

                using var jsonContent = JsonContent.Create(request, options: _jsonOptions);
                using var response = await _httpClient.PostAsync("api/v1/chat", jsonContent);

                if (!response.IsSuccessStatusCode) {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("LLM 오류: {StatusCode}, {Error}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"LLM 서비스 오류: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var llmResponse = JsonSerializer.Deserialize<LLMResponse>(responseContent, _jsonOptions);

                if (llmResponse == null) {
                    _logger.LogDebug("응답 파싱 실패");
                    throw new InvalidOperationException("응답을 파싱할 수 없습니다.");
                }

                _logger.LogDebug("LLM 성공: 토큰 {TotalTokens}, 응답길이 {ResponseLength}",
                    llmResponse.TotalTokens,
                    llmResponse.OutputText?.Length ?? 0);

                return llmResponse;
            }
            catch (Exception ex) {
                _logger.LogDebug(ex, "LLM 요청 처리 중 예외 발생");
                throw;
            }
        }


        public async Task<LLMResponse> CreateTextResponseAsync(
            string systemMessage,
            string userMessage,
            string? instructions = "",
            List<History>? conversationHistory = default,
            string? model = "gpt-4o-mini",
            int? maxTokens = 1000,
            float? temperature = 0.7f)
        {
            var request = new LLMRequest {
                RequestId = Guid.NewGuid().ToString(),
                SystemPrompt = systemMessage,
                UserPrompt = userMessage,
                Instructions = instructions ?? "",
                ConversationHistory = conversationHistory ?? new List<History>(),
                Model = model ?? "gpt-4o-mini",
                MaxTokens = maxTokens ?? 1000,
                Temperature = temperature ?? 0.7f,
                OpenAiApiKey = "",
                UseUserApiKey = false
            };

            return await SendRequestAsync(request);
        }
    }
}
