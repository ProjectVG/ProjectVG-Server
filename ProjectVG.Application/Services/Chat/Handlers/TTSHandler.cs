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
            if (string.IsNullOrWhiteSpace(context.VoiceName) || result.Segments?.Count == 0)
            {
                return;
            }

            var ttsTasks = new List<Task<(int idx, TextToSpeechResponse)>>();
            
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
                if (ttsResult?.Success == true && ttsResult.AudioData != null)
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
        }
    }
} 