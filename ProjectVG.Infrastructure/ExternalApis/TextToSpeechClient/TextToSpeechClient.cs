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
                ValidateRequest(request);
                // 기본 보이스 ID 사용 (설정에서 가져올 수 있음)
                string voiceId = "f4a2a3f41fc82de8616b84";
                
                // request 메시지 생성
                string json = JsonSerializer.Serialize(request);
                _logger.LogInformation("[TextToSpeech][Request JSON] {Json}", json);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                // API 키 헤더 추가 (직접 환경변수에서 읽음)
                var apiKey = Environment.GetEnvironmentVariable("TTSApiKey");
                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new InvalidOperationException("환경 변수 TTSApiKey가 설정되어 있지 않습니다.");
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-sup-api-key", apiKey);

                _logger.LogInformation("[TextToSpeech] TextToSpeech 요청 전송: {Text}", request.Text.Substring(0, Math.Min(50, request.Text.Length)) + "...");
                
                // 요청 전송
                HttpResponseMessage response = await _httpClient.PostAsync($"/v1/text-to-speech/{voiceId}", content);

                var voiceResponse = new TextToSpeechResponse
                {
                    StatusCode = (int)response.StatusCode
                };

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[TextToSpeech] TextToSpeech 응답 실패: {StatusCode}", response.StatusCode);
                    voiceResponse.Success = false;
                    voiceResponse.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                    return voiceResponse;
                }

                // 오디오 데이터 읽기
                voiceResponse.AudioData = await response.Content.ReadAsByteArrayAsync();
                voiceResponse.ContentType = response.Content.Headers.ContentType?.ToString();

                // 오디오 길이 헤더 확인
                if (response.Headers.Contains("X-Audio-Length"))
                {
                    var audioLengthHeader = response.Headers.GetValues("X-Audio-Length").FirstOrDefault();
                    if (float.TryParse(audioLengthHeader, out float audioLength))
                    {
                        voiceResponse.AudioLength = audioLength;
                    }
                }

                _logger.LogInformation("[TextToSpeech] TextToSpeech 응답 생성 완료: 오디오 길이 ({AudioLength:F2}초)", voiceResponse.AudioLength);

                return voiceResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TextToSpeech] TextToSpeech 요청 처리 중 예외 발생");

                return new TextToSpeechResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                };
            }
        }

        private void ValidateRequest(TextToSpeechRequest request)
        {
            // text: 필수, 1~300자
            if (string.IsNullOrWhiteSpace(request.Text))
                throw new ArgumentException("text 필드는 필수이며 비어 있을 수 없습니다.");
            if (request.Text.Length > 300)
                throw new ArgumentException("text 필드는 300자를 초과할 수 없습니다.");

            // language: 필수, ko/en/ja
            var allowedLanguages = new[] { "ko", "en", "ja" };
            if (string.IsNullOrWhiteSpace(request.Language) || !allowedLanguages.Contains(request.Language))
                throw new ArgumentException($"language 필드는 필수이며 {string.Join(", ", allowedLanguages)} 중 하나여야 합니다.");

            // style: 존재 시 허용값(예시: neutral, happy, sad, angry 등. 실제 값은 API 문서/콘솔 참고)
            if (!string.IsNullOrWhiteSpace(request.Style))
            {
                var allowedStyles = new[] { "neutral", "happy", "sad", "angry" }; // 필요시 확장
                if (!allowedStyles.Contains(request.Style))
                    throw new ArgumentException($"style 필드 값 '{request.Style}'는 허용되지 않습니다. 허용값: {string.Join(", ", allowedStyles)}");
            }

            // model: 존재 시 허용값(예시: sona_speech_1)
            if (!string.IsNullOrWhiteSpace(request.Model))
            {
                var allowedModels = new[] { "sona_speech_1" }; // 필요시 확장
                if (!allowedModels.Contains(request.Model))
                    throw new ArgumentException($"model 필드 값 '{request.Model}'는 허용되지 않습니다. 허용값: {string.Join(", ", allowedModels)}");
            }

            // voice_settings: 존재 시 값 범위 체크
            if (request.VoiceSettings != null)
            {
                if (request.VoiceSettings.PitchShift < -12 || request.VoiceSettings.PitchShift > 12)
                    throw new ArgumentException("voice_settings.pitch_shift는 -12~12 범위여야 합니다.");
                if (request.VoiceSettings.PitchVariance < 0.1f || request.VoiceSettings.PitchVariance > 2f)
                    throw new ArgumentException("voice_settings.pitch_variance는 0.1~2 범위여야 합니다.");
                // speed는 음수 불가, 0은 비정상, 일반적으로 0.5~2 정도 사용(문서엔 제한 없음)
                if (request.VoiceSettings.Speed <= 0)
                    throw new ArgumentException("voice_settings.speed는 0보다 커야 합니다.");
            }
        }

        public async Task<TextToSpeechDurationResponse> PredictDurationAsync(TextToSpeechDurationRequest request)
        {
            try
            {
                string endpoint = $"/v1/predict-duration/{request.VoiceId}";

                // request 메시지 생성
                string json = JsonSerializer.Serialize(request);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                // API 키 헤더 추가 (직접 환경변수에서 읽음)
                var apiKey = Environment.GetEnvironmentVariable("TTSApiKey");
                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new InvalidOperationException("환경 변수 TTSApiKey가 설정되어 있지 않습니다.");
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-sup-api-key", apiKey);

                _logger.LogInformation("[TextToSpeech] 지속 시간 예측 요청 전송: {Text}", request.Text.Substring(0, Math.Min(50, request.Text.Length)) + "...");
                
                // 요청 전송
                HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);

                var durationResponse = new TextToSpeechDurationResponse
                {
                    StatusCode = (int)response.StatusCode
                };

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[TextToSpeech] 지속 시간 예측 응답 실패: {StatusCode}", response.StatusCode);
                    durationResponse.Success = false;
                    durationResponse.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                    return durationResponse;
                }

                // 응답 파싱
                var responseBody = await response.Content.ReadAsStringAsync();
                var predictedDuration = JsonSerializer.Deserialize<TextToSpeechDurationResponse>(responseBody);

                if (predictedDuration == null)
                {
                    _logger.LogError("[TextToSpeech] 지속 시간 예측 응답 파싱 실패");
                    durationResponse.Success = false;
                    durationResponse.ErrorMessage = "응답을 파싱할 수 없습니다.";
                    return durationResponse;
                }

                durationResponse.Duration = predictedDuration.Duration;

                _logger.LogInformation("[TextToSpeech] 지속 시간 예측 완료: {Duration:F2}초", durationResponse.Duration);

                return durationResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TextToSpeech] 지속 시간 예측 요청 처리 중 예외 발생");

                return new TextToSpeechDurationResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                };
            }
        }
    }
} 