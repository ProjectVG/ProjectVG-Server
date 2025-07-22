using ProjectVG.Infrastructure.ExternalApis.TextToSpeech.Models;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Infrastructure.ExternalApis.TextToSpeech
{
    public class TextToSpeechClient : ITextToSpeechClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TextToSpeechClient> _logger;

        public TextToSpeechClient(HttpClient httpClient, ILogger<TextToSpeechClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<TextToSpeechResponse> TextToSpeechAsync(TextToSpeechRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.VoiceId))
                    throw new ArgumentException("VoiceId는 필수입니다.");
                string voiceId = request.VoiceId;

                string json = JsonSerializer.Serialize(request);
                _logger.LogDebug("[TTS][Request JSON] {Json}", json);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                var apiKey = Environment.GetEnvironmentVariable("TTSApiKey");
                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new InvalidOperationException("환경 변수 TTSApiKey가 설정되어 있지 않습니다.");
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-sup-api-key", apiKey);

                var startTime = DateTime.UtcNow;
                _logger.LogDebug("[TTS] 요청 전송: {Text}", request.Text.Substring(0, Math.Min(50, request.Text.Length)) + "...");
                HttpResponseMessage response = await _httpClient.PostAsync($"/v1/text-to-speech/{voiceId}", content);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                var voiceResponse = new TextToSpeechResponse
                {
                    StatusCode = (int)response.StatusCode
                };

                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = GetErrorMessageForStatusCode((int)response.StatusCode, response.ReasonPhrase);
                    _logger.LogDebug("[TTS] 응답 실패: {StatusCode} - {ErrorMsg}", response.StatusCode, errorMsg);
                    voiceResponse.Success = false;
                    voiceResponse.ErrorMessage = errorMsg;
                    return voiceResponse;
                }

                voiceResponse.AudioData = await response.Content.ReadAsByteArrayAsync();
                voiceResponse.ContentType = response.Content.Headers.ContentType?.ToString();

                if (response.Headers.Contains("X-Audio-Length"))
                {
                    var audioLengthHeader = response.Headers.GetValues("X-Audio-Length").FirstOrDefault();
                    if (float.TryParse(audioLengthHeader, out float audioLength))
                    {
                        voiceResponse.AudioLength = audioLength;
                    }
                }

                _logger.LogDebug("[TTS][Response] 오디오 길이: {AudioLength:F2}초, ContentType: {ContentType}, 바이트: {Length}, 소요시간: {Elapsed}ms",
                    voiceResponse.AudioLength, voiceResponse.ContentType, voiceResponse.AudioData?.Length ?? 0, elapsed);

                return voiceResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TTS] 요청 처리 중 예외 발생");
                return new TextToSpeechResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                };
            }
        }

        private string GetErrorMessageForStatusCode(int statusCode, string reasonPhrase)
        {
            return $"HTTP {statusCode}: {reasonPhrase}";
        }

        public async Task<TextToSpeechDurationResponse> PredictDurationAsync(TextToSpeechDurationRequest request)
        {
            try
            {
                string endpoint = $"/v1/predict-duration/{request.VoiceId}";
                string json = JsonSerializer.Serialize(request);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                var apiKey = Environment.GetEnvironmentVariable("TTSApiKey");
                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new InvalidOperationException("환경 변수 TTSApiKey가 설정되어 있지 않습니다.");
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-sup-api-key", apiKey);

                var startTime = DateTime.UtcNow;
                _logger.LogDebug("[TTS] 지속 시간 예측 요청 전송: {Text}", request.Text.Substring(0, Math.Min(50, request.Text.Length)) + "...");
                HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                var durationResponse = new TextToSpeechDurationResponse
                {
                    StatusCode = (int)response.StatusCode
                };

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("[TTS] 지속 시간 예측 응답 실패: {StatusCode}", response.StatusCode);
                    durationResponse.Success = false;
                    durationResponse.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                    return durationResponse;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("[TTS][Duration][Response] {Body}, 소요시간: {Elapsed}ms", responseBody, elapsed);
                var predictedDuration = JsonSerializer.Deserialize<TextToSpeechDurationResponse>(responseBody);

                if (predictedDuration == null)
                {
                    _logger.LogDebug("[TTS] 지속 시간 예측 응답 파싱 실패");
                    durationResponse.Success = false;
                    durationResponse.ErrorMessage = "응답을 파싱할 수 없습니다.";
                    return durationResponse;
                }

                durationResponse.Duration = predictedDuration.Duration;
                _logger.LogDebug("[TTS] 지속 시간 예측 완료: {Duration:F2}초", durationResponse.Duration);

                return durationResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TTS] 지속 시간 예측 요청 처리 중 예외 발생");
                return new TextToSpeechDurationResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                };
            }
        }
    }
} 