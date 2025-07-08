using MainAPI_Server.Models.External.LLM;
using System.Text;
using System.Text.Json;

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

        public async Task<LLMResponse> SendRequestAsync(LLMRequest request)
        {
            try
            {
                // request 메시지 생성
                string json = JsonSerializer.Serialize(request);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("[LLM] 요청 전송: {UserMessage}", request.UserMessage.Substring(0, Math.Min(50, request.UserMessage.Length)) + "...");
                
                // 요청 결과 전송
                HttpResponseMessage response = await _httpClient.PostAsync("api/v1/chat", content);

                if (!response.IsSuccessStatusCode) {
                    _logger.LogWarning("[LLM] 응답 실패: {StatusCode}", response.StatusCode);
                    return new LLMResponse {
                        Success = false,
                        ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
                    };
                }

                // 요청 파싱
                var responseBody = await response.Content.ReadAsStringAsync();
                var llmResponse = JsonSerializer.Deserialize<LLMResponse>(responseBody) ?? new LLMResponse();

                if (llmResponse == null)
                {
                    _logger.LogError("[LLM] 응답 파싱 실패");
                    return new LLMResponse
                    {
                        Success = false,
                        ErrorMessage = "응답을 파싱할 수 없습니다."
                    };
                }

                _logger.LogInformation("[LLM] 응답 생성 완료: 토큰 사용량 ({TokensUsed}) , 요청 시간({ProcessingTimeMs:F2}ms)", llmResponse.TokensUsed, llmResponse.ResponseTime);

                return llmResponse;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "[LLM] 요청 처리 중 예외 발생");

                return new LLMResponse {
                    Success = false,
                    ErrorMessage = ex.Message,
                };
            }

        }

    }
} 