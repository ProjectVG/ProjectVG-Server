using ProjectVG.Infrastructure.ExternalApis.TextToSpeech;
using ProjectVG.Infrastructure.ExternalApis.TextToSpeech.Models;
using ProjectVG.Common.Constants;
using Microsoft.Extensions.Logging;
using ProjectVG.Common.Exceptions;

namespace ProjectVG.Application.Services.Voice
{
    public class VoiceService : IVoiceService
    {
        private readonly ITextToSpeechClient _ttsClient;
        private readonly ILogger<VoiceService> _logger;

        public VoiceService(ITextToSpeechClient ttsClient, ILogger<VoiceService> logger)
        {
            _ttsClient = ttsClient;
            _logger = logger;
        }

        public async Task<TextToSpeechResponse> TextToSpeechAsync(
            string voiceName,
            string text,
            string emotion,
            VoiceSettings? voiceSettings = null)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var profile = ValidateVoiceRequest(voiceName, text, emotion);

                var request = new TextToSpeechRequest
                {
                    Text = text,
                    Language = profile.DefaultLanguage,
                    Emotion = emotion,
                    VoiceSettings = voiceSettings ?? new VoiceSettings(),
                    VoiceId = profile.VoiceId
                };

                // API 호출
                var response = await _ttsClient.TextToSpeechAsync(request);

                var endTime = DateTime.UtcNow;
                var processingTime = (endTime - startTime).TotalMilliseconds;

                _logger.LogInformation("[Voice] TTS 응답 생성 완료: 오디오 길이 ({AudioLength:F2}초), 요청 시간({ProcessingTimeMs:F2}ms)",
                    response.AudioLength, processingTime);

                return response;
            }
            catch (ValidationException vex)
            {
                _logger.LogWarning(vex, "[Voice] 요청 검증 실패");
                return new TextToSpeechResponse
                {
                    Success = false,
                    ErrorMessage = vex.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Voice] TTS 서비스 오류 발생");
                return new TextToSpeechResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                };
            }
        }

        private VoiceProfile ValidateVoiceRequest(string voiceName, string text, string style, string language = "ko", VoiceSettings? voiceSettings = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ValidationException("텍스트가 비어있습니다.");
            if (text.Length > 300)
                throw new ValidationException("텍스트는 300자를 초과할 수 없습니다.");

            var allowedLanguages = new[] { "ko", "en", "ja" };
            if (string.IsNullOrWhiteSpace(language) || !allowedLanguages.Contains(language))
                throw new ValidationException($"language는 {string.Join(", ", allowedLanguages)} 중 하나여야 합니다.");

            var profile = VoiceCatalog.GetProfile(voiceName);
            if (profile == null)
                throw new ValidationException($"존재하지 않는 보이스 이름: {voiceName}");

            if (style != null && !profile.SupportedStyles.Contains(style))
                throw new ValidationException($"보이스 '{voiceName}'는 '{style}' 스타일을 지원하지 않습니다.");

            if (voiceSettings != null)
            {
                if (voiceSettings.PitchShift < -12 || voiceSettings.PitchShift > 12)
                    throw new ValidationException("voice_settings.pitch_shift는 -12~12 범위여야 합니다.");
                if (voiceSettings.PitchVariance < 0.1f || voiceSettings.PitchVariance > 2f)
                    throw new ValidationException("voice_settings.pitch_variance는 0.1~2 범위여야 합니다.");
                if (voiceSettings.Speed <= 0)
                    throw new ValidationException("voice_settings.speed는 0보다 커야 합니다.");
            }

            return profile;
        }
    }
} 