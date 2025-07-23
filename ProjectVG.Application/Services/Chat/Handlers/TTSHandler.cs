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
            if (!string.IsNullOrWhiteSpace(context.VoiceName) && result.Text != null && result.Emotion != null && result.Text.Count > 0)
            {
                for (int i = 0; i < result.Text.Count; i++)
                {
                    string text = result.Text[i];
                    string emotion = result.Emotion.Count > i ? result.Emotion[i] : "neutral";
                    try
                    {
                        var ttsResult = await _voiceService.TextToSpeechAsync(
                            context.VoiceName,
                            text,
                            emotion,
                            null // VoiceSettings 확장 가능
                        );
                        if (ttsResult.Success && ttsResult.AudioData != null)
                        {
                            result.AudioDataList.Add(ttsResult.AudioData);
                            result.AudioContentTypeList.Add(ttsResult.ContentType);
                            result.AudioLengthList.Add(ttsResult.AudioLength);
                            // 첫번째 결과만 단일 필드에 세팅 (하위 호환)
                            if (i == 0)
                            {
                                result.AudioData = ttsResult.AudioData;
                                result.AudioContentType = ttsResult.ContentType;
                                result.AudioLength = ttsResult.AudioLength;
                            }
                            // 0.1초당 1Cost, 올림
                            if (ttsResult.AudioLength.HasValue)
                            {
                                result.Cost += Math.Ceiling(ttsResult.AudioLength.Value / 0.1);
                            }
                        }
                        else if (!ttsResult.Success)
                        {
                            _logger.LogWarning("TTS 변환 실패: {Error}", ttsResult.ErrorMessage);
                            result.AudioDataList.Add(null);
                            result.AudioContentTypeList.Add(null);
                            result.AudioLengthList.Add(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "TTS 변환 중 예외 발생");
                        result.AudioDataList.Add(null);
                        result.AudioContentTypeList.Add(null);
                        result.AudioLengthList.Add(null);
                    }
                }
            }
        }
    }
} 