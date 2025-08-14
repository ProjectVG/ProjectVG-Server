using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Voice;
using Microsoft.Extensions.Logging;
using ProjectVG.Infrastructure.Integrations.TextToSpeechClient.Models;

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
            _logger.LogInformation("[TTS 진입 조건] VoiceName={VoiceName}, SegmentsCount={SegmentsCount}",
                context.VoiceName, result.Segments?.Count ?? 0);
            
            if (!string.IsNullOrWhiteSpace(context.VoiceName) && result.Segments != null && result.Segments.Count > 0)
            {
                var ttsTasks = new List<Task<(int idx, TextToSpeechResponse)>>();
                
                // 각 세그먼트에 대해 TTS 요청 생성
                for (int i = 0; i < result.Segments.Count; i++)
                {
                    int idx = i;
                    var segment = result.Segments[idx];
                    
                    if (!segment.HasText)
                    {
                        _logger.LogDebug("텍스트가 없는 세그먼트 건너뜀: idx={Idx}", idx);
                        continue;
                    }
                    
                    string emotion = segment.Emotion ?? "neutral";
                    
                    _logger.LogInformation("[TTS 요청] idx={Idx}, Voice={Voice}, Emotion={Emotion}, Text={Text}", 
                        idx, context.VoiceName, emotion, segment.Text);
                    
                    ttsTasks.Add(Task.Run(async () => (idx, await _voiceService.TextToSpeechAsync(
                        context.VoiceName,
                        segment.Text!,
                        emotion,
                        "ko",
                        null // VoiceSettings 확장 가능
                    ))));
                }
                
                var ttsResults = await Task.WhenAll(ttsTasks);
                
                // idx 기준으로 정렬하여 순서 보장
                foreach (var (idx, ttsResult) in ttsResults.OrderBy(x => x.idx))
                {
                    _logger.LogInformation("[TTS 결과] idx={Idx}, Success={Success}, Error={Error}, AudioLength={AudioLength}", 
                        idx, ttsResult?.Success, ttsResult?.ErrorMessage, ttsResult?.AudioLength);
                    
                    if (ttsResult != null && ttsResult.Success && ttsResult.AudioData != null)
                    {
                        // 기존 세그먼트에 오디오 정보 추가
                        var segment = result.Segments[idx];
                        segment.AudioData = ttsResult.AudioData;
                        segment.AudioContentType = ttsResult.ContentType;
                        segment.AudioLength = ttsResult.AudioLength;
                        
                        // 0.1초당 1Cost, 올림
                        if (ttsResult.AudioLength.HasValue)
                        {
                            result.Cost += Math.Ceiling(ttsResult.AudioLength.Value / 0.1);
                        }
                    }
                    else if (ttsResult == null || !ttsResult.Success)
                    {
                        _logger.LogWarning("TTS 변환 실패: {Error}", ttsResult?.ErrorMessage);
                        // TTS 실패 시에도 텍스트는 그대로 유지
                    }
                }
            }
        }
    }
} 