using ProjectVG.Application.Models.Chat;
using ProjectVG.Infrastructure.Integrations.TextToSpeechClient;
using ProjectVG.Infrastructure.Integrations.TextToSpeechClient.Models;

namespace ProjectVG.Application.Services.Chat.Processors
{
    public class ChatTTSProcessor
    {
        private readonly ITextToSpeechClient _ttsClient;
        private readonly ILogger<ChatTTSProcessor> _logger;

        public ChatTTSProcessor(
            ITextToSpeechClient ttsClient,
            ILogger<ChatTTSProcessor> logger)
        {
            _ttsClient = ttsClient;
            _logger = logger;
        }

        public async Task ProcessAsync(ChatProcessContext context)
        {
            if (string.IsNullOrWhiteSpace(context.Character?.VoiceId) || context.Segments?.Count == 0) {
                _logger.LogDebug("TTS 처리 건너뜀: 세션 {SessionId}, 음성ID {VoiceId}, 세그먼트 수 {SegmentCount}",
                    context.SessionId, context.Character?.VoiceId, context.Segments?.Count ?? 0);
                return;
            }

            var profile = VoiceCatalog.GetProfile(context.Character.VoiceId);
            if (profile == null) {
                _logger.LogWarning("존재하지 않는 보이스: {VoiceId}, 세션 {SessionId}", context.Character.VoiceId, context.SessionId);
                return;
            }

            var ttsTasks = new List<Task<(int idx, TextToSpeechResponse)>>();
            for (int i = 0; i < context.Segments.Count; i++) {
                var segment = context.Segments[i];
                if (!segment.HasText) continue;

                var emotion = NormalizeEmotion(segment.Emotion, profile);
                int idx = i;
                ttsTasks.Add(Task.Run(async () => (idx, await GenerateTTSAsync(profile, segment.Text!, emotion))));
            }

            var ttsResults = await Task.WhenAll(ttsTasks);
            var processedCount = 0;

            foreach (var (idx, ttsResult) in ttsResults.OrderBy(x => x.idx)) {
                if (ttsResult.Success == true && ttsResult.AudioData != null) {
                    var segment = context.Segments[idx];
                    segment.SetAudioData(ttsResult.AudioData, ttsResult.ContentType, ttsResult.AudioLength);
                    
                    if (ttsResult.AudioLength.HasValue) {
                        var ttsCost = TTSCostInfo.CalculateTTSCost(ttsResult.AudioLength.Value);
                        context.AddCost(ttsCost);
                        Console.WriteLine($"[TTS_DEBUG] 오디오 길이: {ttsResult.AudioLength.Value:F2}초, TTS 비용: {ttsCost:F0} Cost");
                    }
                    processedCount++;
                }
            }

            _logger.LogDebug("TTS 처리 완료: 세션 {SessionId}, 처리된 세그먼트 {ProcessedCount}개, 총 비용 {TotalCost}",
                context.SessionId, processedCount, context.Cost);
        }

        private string NormalizeEmotion(string? emotion, VoiceProfile profile)
        {
            var normalizedEmotion = emotion ?? "neutral";
            if (!profile.SupportedStyles.Contains(normalizedEmotion)) {
                _logger.LogWarning("보이스 '{VoiceId}'는 '{Emotion}' 스타일을 지원하지 않습니다. 기본값 사용.",
                    profile.VoiceId, normalizedEmotion);
                return "neutral";
            }
            return normalizedEmotion;
        }

        private async Task<TextToSpeechResponse> GenerateTTSAsync(VoiceProfile profile, string text, string emotion)
        {
            var startTime = DateTime.UtcNow;

            try {
                if (string.IsNullOrWhiteSpace(text))
                    throw new ValidationException(ErrorCode.MESSAGE_EMPTY, "텍스트가 비어있습니다.");
                if (text.Length > 300)
                    throw new ValidationException(ErrorCode.MESSAGE_TOO_LONG, "텍스트는 300자를 초과할 수 없습니다.");

                var request = new TextToSpeechRequest {
                    Text = text,
                    Language = profile.DefaultLanguage,
                    Emotion = emotion,
                    VoiceSettings = new VoiceSettings(),
                    VoiceId = profile.VoiceId
                };

                var response = await _ttsClient.TextToSpeechAsync(request);

                var endTime = DateTime.UtcNow;
                var processingTime = (endTime - startTime).TotalMilliseconds;
                _logger.LogInformation("[TTS] 응답 생성 완료: 오디오 길이 ({AudioLength:F2}초), 요청 시간({ProcessingTimeMs:F2}ms)",
                    response.AudioLength, processingTime);

                return response;
            }
            catch (ValidationException vex) {
                _logger.LogWarning(vex, "[TTS] 요청 검증 실패");
                return new TextToSpeechResponse { Success = false, ErrorMessage = vex.Message };
            }
            catch (Exception ex) {
                _logger.LogError(ex, "[TTS] TTS 서비스 오류 발생");
                return new TextToSpeechResponse { Success = false, ErrorMessage = ex.Message };
            }
        }
    }
}

