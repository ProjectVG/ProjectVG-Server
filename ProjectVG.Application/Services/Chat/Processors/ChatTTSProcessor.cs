using ProjectVG.Application.Models.Chat;
using ProjectVG.Infrastructure.Integrations.TextToSpeechClient;
using ProjectVG.Infrastructure.Integrations.TextToSpeechClient.Models;
using ProjectVG.Common.Constants;
using ProjectVG.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Chat
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

        public async Task ProcessAsync(ChatPreprocessContext context, ChatProcessResult result)
        {
            if (ShouldSkipProcessing(context, result))
            {
                return;
            }

            var profile = GetVoiceProfile(context.VoiceName, context.SessionId);
            if (profile == null)
            {
                return;
            }

            var ttsTasks = CreateTTSProcessingTasks(context, result, profile);
            var ttsResults = await Task.WhenAll(ttsTasks);
            
            ApplyTTSResultsToSegments(ttsResults, result);
            
            LogProcessingCompletion(context.SessionId, ttsResults, result.Cost);
        }

        private bool ShouldSkipProcessing(ChatPreprocessContext context, ChatProcessResult result)
        {
            if (string.IsNullOrWhiteSpace(context.VoiceName) || result.Segments?.Count == 0)
            {
                _logger.LogDebug("TTS 처리 건너뜀: 세션 {SessionId}, 음성명 {VoiceName}, 세그먼트 수 {SegmentCount}",
                    context.SessionId, context.VoiceName, result.Segments?.Count ?? 0);
                return true;
            }
            return false;
        }

        private VoiceProfile? GetVoiceProfile(string voiceName, string sessionId)
        {
            var profile = VoiceCatalog.GetProfile(voiceName);
            if (profile == null)
            {
                _logger.LogWarning("존재하지 않는 보이스: {VoiceName}, 세션 {SessionId}", voiceName, sessionId);
            }
            return profile;
        }

        private List<Task<(int idx, TextToSpeechResponse)>> CreateTTSProcessingTasks(
            ChatPreprocessContext context, 
            ChatProcessResult result, 
            VoiceProfile profile)
        {
            var ttsTasks = new List<Task<(int idx, TextToSpeechResponse)>>();

            for (int i = 0; i < result.Segments.Count; i++)
            {
                int idx = i;
                var segment = result.Segments[idx];

                if (!segment.HasText) continue;

                var emotion = ValidateAndNormalizeEmotion(segment.Emotion, profile, context.VoiceName);
                ttsTasks.Add(Task.Run(async () => (idx, await ProcessTTSAsync(profile, segment.Text!, emotion))));
            }

            return ttsTasks;
        }

        private string ValidateAndNormalizeEmotion(string? emotion, VoiceProfile profile, string voiceName)
        {
            var normalizedEmotion = emotion ?? "neutral";
            
            if (!profile.SupportedStyles.Contains(normalizedEmotion))
            {
                _logger.LogWarning("보이스 '{VoiceName}'는 '{Emotion}' 스타일을 지원하지 않습니다. 기본값 사용.", 
                    voiceName, normalizedEmotion);
                return "neutral";
            }
            
            return normalizedEmotion;
        }

        private void ApplyTTSResultsToSegments(
            (int idx, TextToSpeechResponse)[] ttsResults, 
            ChatProcessResult result)
        {
            foreach (var (idx, ttsResult) in ttsResults.OrderBy(x => x.idx))
            {
                if (IsValidTTSResult(ttsResult))
                {
                    ApplyTTSResultToSegment(result.Segments[idx], ttsResult);
                    UpdateCost(result, ttsResult);
                }
            }
        }

        private bool IsValidTTSResult(TextToSpeechResponse ttsResult)
        {
            return ttsResult.Success == true && ttsResult.AudioData != null;
        }

        private void ApplyTTSResultToSegment(ChatMessageSegment segment, TextToSpeechResponse ttsResult)
        {
            segment.AudioData = ttsResult.AudioData;
            segment.AudioContentType = ttsResult.ContentType;
            segment.AudioLength = ttsResult.AudioLength;
        }

        private void UpdateCost(ChatProcessResult result, TextToSpeechResponse ttsResult)
        {
            if (ttsResult.AudioLength.HasValue)
            {
                result.Cost += Math.Ceiling(ttsResult.AudioLength.Value / 0.1);
            }
        }

        private void LogProcessingCompletion(string sessionId, (int idx, TextToSpeechResponse)[] ttsResults, double totalCost)
        {
            var processedCount = ttsResults.Count(r => r.Item2.Success);
            _logger.LogDebug("TTS 처리 완료: 세션 {SessionId}, 처리된 세그먼트 {ProcessedCount}개, 총 비용 {TotalCost}",
                sessionId, processedCount, totalCost);
        }

        private async Task<TextToSpeechResponse> ProcessTTSAsync(VoiceProfile profile, string text, string emotion)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                ValidateText(text);
                var request = CreateTTSRequest(profile, text, emotion);
                var response = await _ttsClient.TextToSpeechAsync(request);
                
                LogTTSProcessingTime(startTime, response.AudioLength);
                return response;
            }
            catch (ValidationException vex)
            {
                _logger.LogWarning(vex, "[TTS] 요청 검증 실패");
                return CreateErrorResponse(vex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TTS] TTS 서비스 오류 발생");
                return CreateErrorResponse(ex.Message);
            }
        }

        private TextToSpeechRequest CreateTTSRequest(VoiceProfile profile, string text, string emotion)
        {
            return new TextToSpeechRequest
            {
                Text = text,
                Language = profile.DefaultLanguage,
                Emotion = emotion,
                VoiceSettings = new VoiceSettings(),
                VoiceId = profile.VoiceId
            };
        }

        private void LogTTSProcessingTime(DateTime startTime, double? audioLength)
        {
            var endTime = DateTime.UtcNow;
            var processingTime = (endTime - startTime).TotalMilliseconds;

            _logger.LogInformation("[TTS] 응답 생성 완료: 오디오 길이 ({AudioLength:F2}초), 요청 시간({ProcessingTimeMs:F2}ms)",
                audioLength, processingTime);
        }

        private TextToSpeechResponse CreateErrorResponse(string errorMessage)
        {
            return new TextToSpeechResponse
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        private void ValidateText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ValidationException(ErrorCode.MESSAGE_EMPTY, "텍스트가 비어있습니다.");
            if (text.Length > 300)
                throw new ValidationException(ErrorCode.MESSAGE_TOO_LONG, "텍스트는 300자를 초과할 수 없습니다.");
        }
    }
}

