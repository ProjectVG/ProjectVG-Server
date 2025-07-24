using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Voice;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.ExternalApis.TextToSpeech.Models;

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
            // TODO: 병렬 TTS 처리에서 각 요청별로 try/catch로 예외를 개별적으로 처리하여,
            // 하나의 요청이 실패해도 전체 Task.WhenAll이 중단되지 않고,
            // 모든 요청의 결과가 도착한 뒤 실패한 요청만 개별적으로 처리할 수 있도록 개선할 것.
            // 예시:
            // ttsTasks.Add(Task.Run(async () => {
            //     try {
            //         var ttsResult = await _voiceService.TextToSpeechAsync(...);
            //         return (idx, ttsResult);
            //     } catch (Exception ex) {
            //         _logger.LogError(ex, $"TTS 변환 중 예외 발생 (idx={idx})");
            //         return (idx, null as TextToSpeechResponse);
            //     }
            // }));
            
            _logger.LogInformation("[TTS 진입 조건] VoiceName={VoiceName}, TextNull={TextNull}, EmotionNull={EmotionNull}, TextCount={TextCount}",
                context.VoiceName, result.Text == null, result.Emotion == null, result.Text?.Count ?? -1);
            if (!string.IsNullOrWhiteSpace(context.VoiceName) && result.Text != null && result.Emotion != null && result.Text.Count > 0)
            {
                var ttsTasks = new List<Task<(int idx, TextToSpeechResponse)>>();
                for (int i = 0; i < result.Text.Count; i++)
                {
                    int idx = i;
                    string text = result.Text[idx];
                    string emotion = result.Emotion.Count > idx ? result.Emotion[idx] : "neutral";
                    _logger.LogInformation("[TTS 요청] idx={Idx}, Voice={Voice}, Emotion={Emotion}, Text={Text}", idx, context.VoiceName, emotion, text);
                    ttsTasks.Add(Task.Run(async () => (idx, await _voiceService.TextToSpeechAsync(
                        context.VoiceName,
                        text,
                        emotion,
                        "ko",
                        null // VoiceSettings 확장 가능
                    ))));
                }
                var ttsResults = await Task.WhenAll(ttsTasks);
                // idx 기준으로 정렬 (혹시라도 순서가 어긋날 경우 대비)
                foreach (var (idx, ttsResult) in ttsResults.OrderBy(x => x.idx))
                {
                    _logger.LogInformation("[TTS 결과] idx={Idx}, Success={Success}, Error={Error}, AudioLength={AudioLength}", idx, ttsResult?.Success, ttsResult?.ErrorMessage, ttsResult?.AudioLength);
                    if (ttsResult != null && ttsResult.Success && ttsResult.AudioData != null)
                    {
                        result.AudioDataList.Add(ttsResult.AudioData);
                        result.AudioContentTypeList.Add(ttsResult.ContentType);
                        result.AudioLengthList.Add(ttsResult.AudioLength);
                        // 첫번째 결과만 단일 필드에 세팅 (하위 호환)
                        if (idx == 0)
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
                    else if (ttsResult == null || !ttsResult.Success)
                    {
                        _logger.LogWarning("TTS 변환 실패: {Error}", ttsResult?.ErrorMessage);
                        result.AudioDataList.Add(null);
                        result.AudioContentTypeList.Add(null);
                        result.AudioLengthList.Add(null);
                    }
                }
            }
        }
    }
} 