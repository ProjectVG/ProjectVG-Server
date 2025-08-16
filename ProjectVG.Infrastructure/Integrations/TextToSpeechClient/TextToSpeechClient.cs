using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.TextToSpeechClient.Models;

namespace ProjectVG.Infrastructure.Integrations.TextToSpeechClient
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

        /// <summary>
        /// 지정한 음성(VoiceId)으로 텍스트를 음성 오디오로 변환하여 결과를 반환합니다.
        /// </summary>
        /// <param name="request">변환할 텍스트 및 음성 설정을 포함한 요청 객체. request.VoiceId는 필수입니다.</param>
        /// <returns>
        /// TextToSpeechResponse 객체를 포함한 비동기 작업.
        /// 성공 시 AudioData(바이트 배열), ContentType, AudioLength, StatusCode 등이 채워집니다.
        /// 실패 또는 예외 발생 시 Success는 false이고 ErrorMessage에 원인이 설정됩니다.
        /// </returns>
        /// <exception cref="ArgumentException">request.VoiceId가 null, 빈 문자열 또는 공백인 경우 던져집니다.</exception>
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
                    string errorMsg = GetErrorMessageForStatusCode((int)response.StatusCode, response.ReasonPhrase ?? "Unknown");
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