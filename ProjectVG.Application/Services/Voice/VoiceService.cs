using ProjectVG.Infrastructure.ExternalApis.TextToSpeech;
using ProjectVG.Infrastructure.ExternalApis.TextToSpeech.Models;
using ProjectVG.Common.Constants;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Voice
{
    public class VoiceValidationException : Exception
    {
        public VoiceValidationException(string message) : base(message) { }
    }

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
            string sty,
            VoiceSettings? voiceSettings = null)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var profile = ValidateVoiceRequest(voiceName, text);

                var request = new TextToSpeechRequest
                {
                    Text = text,
                    Language = profile.DefaultLanguage,
                    Style = sty,
                    Model = profile.Model,
                    VoiceSettings = voiceSettings ?? new VoiceSettings()
                };

                // API 호출
                var response = await _ttsClient.TextToSpeechAsync(request with { VoiceId = profile.VoiceId });

                var endTime = DateTime.UtcNow;
                var processingTime = (endTime - startTime).TotalMilliseconds;

                _logger.LogInformation("[Voice] TTS 응답 생성 완료: 오디오 길이 ({AudioLength:F2}초), 요청 시간({ProcessingTimeMs:F2}ms)",
                    response.AudioLength, processingTime);

                return response;
            }
            catch (VoiceValidationException vex)
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

        private VoiceProfile ValidateVoiceRequest(string voiceName, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new VoiceValidationException("텍스트가 비어있습니다.");

            var profile = VoiceCatalog.GetProfile(voiceName);
            if (profile == null)
                throw new VoiceValidationException($"존재하지 않는 보이스 이름: {voiceName}");

            if (style != null && !profile.SupportedStyles.Contains(style))
                throw new VoiceValidationException($"보이스 '{voiceName}'는 '{style}' 스타일을 지원하지 않습니다.");

            return profile;
        }
    }
} 