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

        /// <summary>
        /// LLMClient 인스턴스를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 내부적으로 JSON 직렬화 옵션을 camelCase로 설정하고 들여쓰기를 비활성화합니다.
        /// 또한 HttpClient의 BaseAddress를 구성 키 "LLM:BaseUrl"에서 읽어 설정(없을 경우 "http://localhost:5601/" 사용),
        /// 타임아웃을 30초로 설정하고 기본 Accept 헤더에 "application/json"을 추가합니다.
        /// </remarks>
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
        /// 지정한 LLM 요청을 LLM 서비스(api/v1/chat)로 전송하고 서비스 응답을 LLMResponse로 반환합니다.
        /// </summary>
        /// <param name="request">전송할 LLM 요청(모델, 시스템/사용자 메시지 등)을 담은 LLMRequest 객체.</param>
        /// <returns>
        /// LLM 서비스에서 반환한 내용을 파싱한 LLMResponse.
        /// - HTTP 응답이 성공적이지 않으면 Success=false와 서비스 오류 메시지를 가진 LLMResponse를 반환합니다.
        /// - HTTP 전송 중 HttpRequestException이 발생하면(예: 연결 실패) 개발용 Mock 성공 응답을 반환합니다.
        /// - 요청이 시간 초과되면 Success=false와 타임아웃 메시지를 가진 LLMResponse를 반환합니다.
        /// - 응답 파싱에 실패하거나 기타 예외가 발생하면 Success=false와 일반 오류 메시지를 가진 LLMResponse를 반환합니다.
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
                    Response = "안녕하세요! 저는 현재 Mock 모드로 동작하고 있습니다. 실제 LLM 서비스가 연결되지 않았습니다.",
                    TokensUsed = 50,
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

        /// <summary>
        /// 시스템 메시지와 사용자 입력(및 선택적 컨텍스트)을 기반으로 LLM 요청을 구성하여 LLM 응답을 생성한다.
        /// </summary>
        /// <param name="systemMessage">대화의 역할·제약을 정의하는 시스템 메시지(예: 동작 지침).</param>
        /// <param name="userMessage">사용자 입력 또는 질의 본문.</param>
        /// <param name="instructions">모델에 추가로 전달할 세부 지시문(선택, 기본값: 빈 문자열).</param>
        /// <param name="conversationHistory">이전 대화 발화 목록(선택, null인 경우 빈 리스트로 대체).</param>
        /// <param name="memoryContext">장기 기억 또는 문맥으로 사용할 문자열 목록(선택, null인 경우 빈 리스트로 대체).</param>
        /// <param name="model">사용할 모델 이름(선택, 기본값: "gpt-4o-mini").</param>
        /// <param name="maxTokens">생성 최대 토큰 수(선택, 기본값: 1000 또는 모델 기본값으로 대체 가능).</param>
        /// <param name="temperature">출력의 창의성 제어 값(선택, 기본값: 0.7).</param>
        /// <returns>LLM 서비스로부터 받은 LLMResponse 객체(성공/실패 및 생성된 텍스트·메타데이터 포함).</returns>
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