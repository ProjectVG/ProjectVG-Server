using ProjectVG.Application.Models.Chat;
using ProjectVG.Infrastructure.Integrations.TextToSpeechClient;
using ProjectVG.Infrastructure.Integrations.TextToSpeechClient.Models;

namespace ProjectVG.Application.Services.Chat.Processors
{
    public class ChatTTSProcessor
    {
        private readonly ITextToSpeechClient _ttsClient;
        private readonly ILogger<ChatTTSProcessor> _logger;

        /// <summary>
        /// ChatTTSProcessor 인스턴스를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 외부 TTS 클라이언트와 로깅 서비스를 주입 받아 내부 필드에 저장합니다.
        /// </remarks>
        public ChatTTSProcessor(
            ITextToSpeechClient ttsClient,
            ILogger<ChatTTSProcessor> logger)
        {
            _ttsClient = ttsClient;
            _logger = logger;
        }

        /// <summary>
        /// 채팅 처리 컨텍스트의 세그먼트들에 대해 음성 합성(TTS)을 비동기적으로 생성하고 결과를 적용한다.
        /// </summary>
        /// <param name="context">TTS 사용 여부(UseTTS), 대상 캐릭터(VoiceId 포함), 세그먼트 목록 및 비용 누적을 포함하는 처리 컨텍스트. 성공한 TTS 오디오는 각 세그먼트에 설정되고(SetAudioData), 오디오 길이에 따라 비용이 계산되어 context.AddCost로 누적된다.</param>
        public async Task ProcessAsync(ChatProcessContext context)
        {
            if (!context.UseTTS || string.IsNullOrWhiteSpace(context.Character?.VoiceId) || context.Segments?.Count == 0) {
                _logger.LogDebug("TTS 처리 건너뜀: 세션 {UserId}, TTS사용여부 {UseTTS}, 음성ID {VoiceId}, 세그먼트 수 {SegmentCount}",
                    context.SessionId, context.UseTTS, context.Character?.VoiceId, context.Segments?.Count ?? 0);
                return;
            }

            var profile = VoiceCatalog.GetProfile(context.Character.VoiceId);
            if (profile == null) {
                _logger.LogWarning("존재하지 않는 보이스: {VoiceId}, 세션 {UserId}", context.Character.VoiceId, context.SessionId);
                return;
            }

            var ttsTasks = new List<Task<(int idx, TextToSpeechResponse)>>();
            for (int i = 0; i < context.Segments?.Count; i++) {
                var segment = context.Segments[i];
                if (!segment.HasContent || segment.IsActionSegment) continue;

                var emotion = NormalizeEmotion(segment.Emotion, profile);
                int idx = i;
                ttsTasks.Add(Task.Run(async () => (idx, await GenerateTTSAsync(profile, segment.Content!, emotion))));
            }

            var ttsResults = await Task.WhenAll(ttsTasks);
            var processedCount = 0;

            foreach (var (idx, ttsResult) in ttsResults.OrderBy(x => x.idx)) {
                if (ttsResult.Success == true && ttsResult.AudioData != null) {
                    var segment = context.Segments?[idx];
                    if (segment != null && context.Segments != null) {
                        context.Segments[idx] = segment.WithAudioData(ttsResult.AudioData, ttsResult.ContentType!, ttsResult.AudioLength ?? 0f);
                    }
                    
                    if (ttsResult.AudioLength.HasValue) {
                        var ttsCost = TTSCostInfo.CalculateTTSCost(ttsResult.AudioLength.Value);
                        context.AddCost(ttsCost);
                        Console.WriteLine($"[TTS_DEBUG] 오디오 길이: {ttsResult.AudioLength.Value:F2}초, TTS 비용: {ttsCost:F0} Cost");
                    }
                    processedCount++;
                }
            }

            _logger.LogDebug("TTS 처리 완료: 세션 {UserId}, 처리된 세그먼트 {ProcessedCount}개, 총 비용 {TotalCost}",
                context.SessionId, processedCount, context.Cost);
        }

        /// <summary>
        /// 주어진 감정 문자열을 정규화하여 음성 프로필이 지원하는 스타일로 반환합니다.
        /// </summary>
        /// <param name="emotion">정규화할 감정 문자열(널이면 "neutral"로 간주).</param>
        /// <param name="profile">대상 음성의 VoiceProfile(지원되는 스타일 목록을 검사함).</param>
        /// <returns>프로필에서 지원하는 감정 스타일이거나, 지원하지 않으면 "neutral".</returns>
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

        /// <summary>
        /// 지정한 음성 프로파일과 감정으로 텍스트를 음성으로 변환해 TTS 응답을 반환합니다.
        /// </summary>
        /// <remarks>
        /// 입력 텍스트는 비어 있거나 공백일 수 없으며 최대 300자까지 허용됩니다. 검증 실패나 호출 중 예외가 발생하면
        /// Success = false 및 ErrorMessage가 설정된 TextToSpeechResponse를 반환합니다.
        /// 외부 TTS 클라이언트에 요청을 보내며, 성공 시 서버가 반환한 오디오 메타데이터(길이 등)를 포함한 응답을 반환합니다.
        /// </remarks>
        /// <param name="profile">사용할 음성 프로파일(기본 언어와 VoiceId가 요청에 사용됨).</param>
        /// <param name="text">변환할 텍스트(공백 이외의 문자 필요, 최대 300자).</param>
        /// <param name="emotion">요청할 감정 스타일(프로필의 지원 스타일에 맞추어 전달되어야 함).</param>
        /// <returns>외부 TTS 호출 결과를 나타내는 TextToSpeechResponse 객체. 실패 시 Success=false와 ErrorMessage가 설정됩니다.</returns>
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

