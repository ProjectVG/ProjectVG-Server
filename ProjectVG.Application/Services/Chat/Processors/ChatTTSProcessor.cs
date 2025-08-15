using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Voice;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatTTSProcessor
    {
        private readonly IVoiceService _voiceService;
        private readonly ILogger<ChatTTSProcessor> _logger;

        public ChatTTSProcessor(
            IVoiceService voiceService,
            ILogger<ChatTTSProcessor> logger)
        {
            _voiceService = voiceService;
            _logger = logger;
        }

        public async Task ProcessAsync(ChatPreprocessContext context, ChatProcessResult result)
        {
            if (string.IsNullOrWhiteSpace(context.VoiceName) || result.Segments?.Count == 0)
            {
                _logger.LogDebug("TTS 처리 건너뜀: 세션 {SessionId}, 음성명 {VoiceName}, 세그먼트 수 {SegmentCount}",
                    context.SessionId, context.VoiceName, result.Segments?.Count ?? 0);
                return;
            }

            var ttsTasks = new List<Task<(int idx, ProjectVG.Infrastructure.Integrations.TextToSpeechClient.Models.TextToSpeechResponse)>>();

            for (int i = 0; i < result.Segments.Count; i++)
            {
                int idx = i;
                var segment = result.Segments[idx];

                if (!segment.HasText) continue;

                string emotion = segment.Emotion ?? "neutral";
                ttsTasks.Add(Task.Run(async () => (idx, await _voiceService.TextToSpeechAsync(
                    context.VoiceName,
                    segment.Text!,
                    emotion,
                    "ko",
                    null
                ))));
            }

            var ttsResults = await Task.WhenAll(ttsTasks);

            foreach (var (idx, ttsResult) in ttsResults.OrderBy(x => x.idx))
            {
                if (ttsResult.Success == true && ttsResult.AudioData != null)
                {
                    var segment = result.Segments[idx];
                    segment.AudioData = ttsResult.AudioData;
                    segment.AudioContentType = ttsResult.ContentType;
                    segment.AudioLength = ttsResult.AudioLength;

                    if (ttsResult.AudioLength.HasValue)
                    {
                        result.Cost += Math.Ceiling(ttsResult.AudioLength.Value / 0.1);
                    }
                }
            }

            _logger.LogDebug("TTS 처리 완료: 세션 {SessionId}, 처리된 세그먼트 {ProcessedCount}개, 총 비용 {TotalCost}",
                context.SessionId, ttsResults.Count(r => r.Item2.Success), result.Cost);
        }
    }
}
