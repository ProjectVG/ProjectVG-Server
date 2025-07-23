using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Voice;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Chat.Handlers
{
    public class TTSHandler
    {
        private readonly IVoiceService _voiceService;
        private readonly ILogger<TTSHandler> _logger;

        public TTSHandler(IVoiceService voiceService, ILogger<TTSHandler> logger)
        {
            _voiceService = voiceService;
            _logger = logger;
        }

        public async Task ApplyTTSAsync(ChatPreprocessContext context, ChatProcessResult result)
        {
            if (!string.IsNullOrWhiteSpace(context.VoiceName) && !string.IsNullOrWhiteSpace(result.Response))
            {
                try
                {
                    var ttsResult = await _voiceService.TextToSpeechAsync(
                        context.VoiceName,
                        result.Response,
                        result.Emotion,
                        null // VoiceSettings 확장 가능
                    );
                    if (ttsResult.Success && ttsResult.AudioData != null)
                    {
                        result.AudioData = ttsResult.AudioData;
                        result.AudioContentType = ttsResult.ContentType;
                        result.AudioLength = ttsResult.AudioLength;
                        // 0.1초당 1Cost, 올림
                        if (result.AudioLength.HasValue)
                        {
                            result.Cost += Math.Ceiling(result.AudioLength.Value / 0.1);
                        }
                    }
                    else if (!ttsResult.Success)
                    {
                        _logger.LogWarning("TTS 변환 실패: {Error}", ttsResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "TTS 변환 중 예외 발생");
                }
            }
        }
    }
} 