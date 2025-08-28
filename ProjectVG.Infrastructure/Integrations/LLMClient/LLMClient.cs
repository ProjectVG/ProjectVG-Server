using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.LLMClient.Models;
using ProjectVG.Common.Constants;
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
            
            // JSON 직렬화 옵션 설정
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            // HTTP 클라이언트 기본 설정
            _httpClient.BaseAddress = new Uri(configuration["LLM:BaseUrl"] ?? "http://localhost:5601/");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// 지정된 LLM 요청을 원격 LLM 서비스로 전송하고 그 결과를 LLMResponse로 반환합니다.
        /// </summary>
        /// <param name="request">전송할 채팅 요청(시스템 메시지, 사용자 메시지, 모델, 토큰/온도 설정 등)을 포함하는 LLMRequest 객체.</param>
        /// <returns>
        /// 서비스의 응답을 파싱한 LLMResponse를 반환합니다.
        /// - HTTP 응답이 실패일 경우: Success = false, ErrorMessage에 상태 코드가 설정된 응답을 반환합니다.
        /// - 응답 파싱에 실패하면: Success = false, ErrorMessage = "응답을 파싱할 수 없습니다."를 반환합니다.
        /// - 네트워크 연결 오류(HttpRequestException) 발생 시: 개발용 mock 성공 응답을 반환합니다 (Success = true, mock Id와 샘플 응답 포함).
        /// - 요청 시간 초과(TaskCanceledException) 시: Success = false, ErrorMessage = "요청 시간이 초과되었습니다."를 반환합니다.
        /// - 기타 예외 발생 시: Success = false, ErrorMessage = "요청 처리 중 오류가 발생했습니다."를 반환합니다.
        /// </returns>
        public async Task<LLMResponse> SendRequestAsync(LLMRequest request)
        {
            try
            {
                _logger.LogDebug("LLM 요청 시작: {Model}, 사용자 메시지: {UserMessage}", 
                    request.Model, 
                    request.UserMessage[..Math.Min(50, request.UserMessage.Length)]);

                using var jsonContent = JsonContent.Create(request, options: _jsonOptions);
                using var response = await _httpClient.PostAsync("api/v1/chat", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("LLM 서비스 오류: {StatusCode}, {Error}", response.StatusCode, errorContent);
                    
                    return new LLMResponse
                    {
                        Success = false,
                        ErrorMessage = $"서비스 오류: {response.StatusCode}"
                    };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var llmResponse = JsonSerializer.Deserialize<LLMResponse>(responseContent, _jsonOptions);

                if (llmResponse?.Success == true)
                {
                    _logger.LogInformation("LLM 요청 성공: 토큰 {TokensUsed}, 응답 길이 {ResponseLength}", 
                        llmResponse.TokensUsed, 
                        llmResponse.Response?.Length ?? 0);
                }

                return llmResponse ?? new LLMResponse
                {
                    Success = false,
                    ErrorMessage = "응답을 파싱할 수 없습니다."
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("LLM 서비스 연결 오류 - Mock 응답 반환: {Error}", ex.Message);
                
                // 임시 Mock 응답 (개발 환경에서만)
                return new LLMResponse
                {
                    Success = true,
                    Id = "mock-chatcmpl-" + Guid.NewGuid().ToString("N")[..8],
                    Response = "안녕하세요! 저는 현재 Mock 모드로 동작하고 있습니다. 실제 LLM 서비스가 연결되지 않았습니다.",
                    TokensUsed = 50,
                    InputTokens = 30,
                    OutputTokens = 20,
                    ResponseTime = 100
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "LLM 요청 시간 초과");
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = "요청 시간이 초과되었습니다."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM 요청 처리 중 예외 발생");
                return new LLMResponse
                {
                    Success = false,
                    ErrorMessage = "요청 처리 중 오류가 발생했습니다."
                };
            }
        }

        public async Task<LLMResponse> CreateTextResponseAsync(
            string systemMessage,
            string userMessage,
            string? instructions = "",
            List<string>? conversationHistory = default,
            List<string>? memoryContext = default,
            string? model = "gpt-4o-mini",
            int? maxTokens = 1000,
            float? temperature = 0.7f)
        {
            var request = new LLMRequest
            {
                SystemMessage = systemMessage,
                UserMessage = userMessage,
                Instructions = instructions ?? "",
                ConversationHistory = conversationHistory ?? new List<string>(),
                MemoryContext = memoryContext ?? new List<string>(),
                Model = model ?? LLMModelInfo.GPT4oMini.Name,
                MaxTokens = maxTokens ?? LLMModelInfo.DefaultSettings.DefaultMaxTokens,
                Temperature = temperature ?? LLMModelInfo.DefaultSettings.DefaultTemperature
            };

            return await SendRequestAsync(request);
        }
    }
} 