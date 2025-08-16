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
        /// ChatTTSProcessor의 인스턴스를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// 내부적으로 제공된 TTS 클라이언트와 로거를 사용하여 채팅 세그먼트의 텍스트를 음성으로 변환하는 처리를 수행합니다.
        /// </remarks>
        public ChatTTSProcessor(
            ITextToSpeechClient ttsClient,
            ILogger<ChatTTSProcessor> logger)
        {
            _ttsClient = ttsClient;
            _logger = logger;
        }

        /// <summary>
        /// 지정된 채팅 전처리 컨텍스트와 처리 결과에 대해 세그먼트별 텍스트-투-음성(TTS) 처리를 수행합니다.
        /// </summary>
        /// <param name="context">TTS 처리에 필요한 세션·음성 이름·기타 설정을 가진 전처리 컨텍스트.</param>
        /// <param name="result">세그먼트 목록을 포함하는 처리 결과 객체; 유효한 TTS 출력은 이 객체의 세그먼트와 비용에 반영됩니다.</param>
        /// <returns>비동기 작업을 나타내는 <see cref="Task"/>. 처리 중 세그먼트가 없거나 음성 프로필을 찾을 수 없으면 즉시 완료됩니다.</returns>
        /// <remarks>
        /// 이 메서드는 다음 동작을 수행합니다:
        /// - 음성 이름이 비어 있거나 세그먼트가 없으면 즉시 반환합니다.
        /// - 음성 프로필을 조회하여 없으면 반환합니다.
        /// - 각 텍스트 세그먼트에 대해 병렬로 TTS 호출을 실행하고, 유효한 결과만 해당 세그먼트에 적용하여 오디오 데이터·콘텐츠 타입·길이를 설정합니다.
        /// - 적용된 오디오 길이에 따라 처리 비용을 누적합니다.
        /// </remarks>
        public async Task ProcessAsync(ChatPreprocessContext context, ChatProcessResult result)
        {
            if (ShouldSkipProcessing(context, result)) {
                return;
            }

            var profile = GetVoiceProfile(context.VoiceName, context.SessionId);
            if (profile == null) {
                return;
            }

            var ttsTasks = CreateTTSProcessingTasks(context, result, profile);
            var ttsResults = await Task.WhenAll(ttsTasks);

            ApplyTTSResultsToSegments(ttsResults, result);

            LogProcessingCompletion(context.SessionId, ttsResults, result.Cost);
        }

        /// <summary>
        /// 채팅 TTS 처리의 실행 여부를 판단한다.
        /// </summary>
        /// <remarks>
        /// 다음 두 조건 중 하나라도 만족하면 처리하지 않고 true를 반환한다:
        /// - context.VoiceName이 null이거나 빈 문자열(공백 포함)인 경우
        /// - result.Segments가 null이거나 요소가 없는 경우
        /// 이 경우 디버그 로그를 남긴다.
        /// </remarks>
        /// <param name="context">현재 전처리 컨텍스트(세션 ID 및 요청된 음성명 포함).</param>
        /// <param name="result">처리 대상인 채팅 결과(세그먼트 목록 포함).</param>
        /// <returns>처리를 건너뛰어야 하면 true, 그렇지 않으면 false.</returns>
        private bool ShouldSkipProcessing(ChatPreprocessContext context, ChatProcessResult result)
        {
            if (string.IsNullOrWhiteSpace(context.VoiceName) || result.Segments?.Count == 0) {
                _logger.LogDebug("TTS 처리 건너뜀: 세션 {SessionId}, 음성명 {VoiceName}, 세그먼트 수 {SegmentCount}",
                    context.SessionId, context.VoiceName, result.Segments?.Count ?? 0);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 음성 이름으로 음성 프로파일을 조회합니다. 프로파일이 없으면 경고 로그를 남기고 null을 반환합니다.
        /// </summary>
        /// <param name="voiceName">조회할 음성의 식별 이름.</param>
        /// <param name="sessionId">로그에 포함할 세션 식별자(없으면 디버깅 목적).</param>
        /// <returns>찾은 VoiceProfile 객체 또는 존재하지 않으면 null.</returns>
        private VoiceProfile? GetVoiceProfile(string voiceName, string sessionId)
        {
            var profile = VoiceCatalog.GetProfile(voiceName);
            if (profile == null) {
                _logger.LogWarning("존재하지 않는 보이스: {VoiceName}, 세션 {SessionId}", voiceName, sessionId);
            }
            return profile;
        }

        /// <summary>
        /// 주어진 채팅 처리 결과의 텍스트가 있는 각 세그먼트에 대해 비동기 TTS 처리 작업을 생성합니다.
        /// </summary>
        /// <remarks>
        /// 각 작업은 해당 세그먼트의 인덱스와 TextToSpeechResponse를 튜플로 반환합니다. 생성되는 작업은 세그먼트의 텍스트가 존재하는 경우에만 추가되며, 실제 실행 및 결과 적용은 호출측에서 병렬로 대기(Task.WhenAll)하고 인덱스를 사용해 원래 세그먼트에 매핑해야 합니다.
        /// </remarks>
        /// <param name="context">현재 전처리 컨텍스트(예: VoiceName 등)에 대한 추가 문맥을 제공합니다.</param>
        /// <param name="result">처리 대상인 채팅 세그먼트 컬렉션을 포함합니다.</param>
        /// <param name="profile">선택된 음성 프로파일(지원 스타일 검증에 사용).</param>
        /// <returns>각 항목이 (세그먼트 인덱스, TextToSpeechResponse)을 반환하는 비동기 작업 목록.</returns>
        private List<Task<(int idx, TextToSpeechResponse)>> CreateTTSProcessingTasks(
            ChatPreprocessContext context,
            ChatProcessResult result,
            VoiceProfile profile)
        {
            var ttsTasks = new List<Task<(int idx, TextToSpeechResponse)>>();

            for (int i = 0; i < result.Segments.Count; i++) {
                int idx = i;
                var segment = result.Segments[idx];

                if (!segment.HasText) continue;

                var emotion = ValidateAndNormalizeEmotion(segment.Emotion, profile, context.VoiceName);
                ttsTasks.Add(Task.Run(async () => (idx, await ProcessTTSAsync(profile, segment.Text!, emotion))));
            }

            return ttsTasks;
        }

        /// <summary>
        /// 입력된 감정 스타일을 검증하고 음성 프로파일에서 지원되는 형식으로 정규화하여 반환합니다.
        /// </summary>
        /// <param name="emotion">사용자가 요청한 감정 스타일(널 허용). 널이거나 프로파일에서 미지원 시 기본값 "neutral"이 반환됩니다.</param>
        /// <param name="profile">감정 스타일의 유효성을 검사할 대상 음성 프로파일.</param>
        /// <param name="voiceName">로그 메시지에 포함되는 보이스 이름(문맥 정보 제공).</param>
        /// <returns>프로파일에서 지원되는 감정 스타일 문자열(지원되지 않으면 "neutral").</returns>
        private string ValidateAndNormalizeEmotion(string? emotion, VoiceProfile profile, string voiceName)
        {
            var normalizedEmotion = emotion ?? "neutral";

            if (!profile.SupportedStyles.Contains(normalizedEmotion)) {
                _logger.LogWarning("보이스 '{VoiceName}'는 '{Emotion}' 스타일을 지원하지 않습니다. 기본값 사용.",
                    voiceName, normalizedEmotion);
                return "neutral";
            }

            return normalizedEmotion;
        }

        /// <summary>
        /// TTS 응답 배열을 대응하는 채팅 세그먼트에 적용하고 비용을 갱신합니다.
        /// </summary>
        /// <remarks>
        /// 전달된 TTS 결과를 인덱스(idx) 기준으로 정렬한 뒤, 성공적이고 오디오 데이터를 포함하는 결과만 대상 세그먼트에 적용합니다.
        /// 유효하지 않은 결과는 무시되며, 각 적용 시점에 result의 비용은 해당 오디오 길이에 따라 갱신됩니다.
        /// </remarks>
        /// <param name="ttsResults">세그먼트 인덱스와 해당하는 TTS 응답의 튜플 배열. 인덱스는 result.Segments의 위치를 가리켜야 합니다.</param>
        /// <param name="result">TTS 결과를 적용할 대상 ChatProcessResult(세그먼트 목록과 비용 필드 포함).</param>
        private void ApplyTTSResultsToSegments(
            (int idx, TextToSpeechResponse)[] ttsResults,
            ChatProcessResult result)
        {
            foreach (var (idx, ttsResult) in ttsResults.OrderBy(x => x.idx)) {
                if (IsValidTTSResult(ttsResult)) {
                    ApplyTTSResultToSegment(result.Segments[idx], ttsResult);
                    UpdateCost(result, ttsResult);
                }
            }
        }

        /// <summary>
        /// TextToSpeech 응답이 성공 상태이며 오디오 데이터를 포함하는지 확인합니다.
        /// </summary>
        /// <param name="ttsResult">검사할 TextToSpeech 응답 객체.</param>
        /// <returns>응답이 성공(Success == true)하고 AudioData가 null이 아니면 true, 그렇지 않으면 false.</returns>
        private bool IsValidTTSResult(TextToSpeechResponse ttsResult)
        {
            return ttsResult.Success == true && ttsResult.AudioData != null;
        }

        /// <summary>
        /// 텍스트-음성 변환 응답의 오디오 정보를 지정된 채팅 세그먼트에 적용합니다.
        /// </summary>
        /// <param name="segment">오디오 정보를 설정할 대상 채팅 메시지 세그먼트. 기존 오디오 필드는 덮어씁니다.</param>
        /// <param name="ttsResult">오디오 데이터, 콘텐츠 타입, 길이를 포함한 TTS 응답.</param>
        private void ApplyTTSResultToSegment(ChatMessageSegment segment, TextToSpeechResponse ttsResult)
        {
            segment.AudioData = ttsResult.AudioData;
            segment.AudioContentType = ttsResult.ContentType;
            segment.AudioLength = ttsResult.AudioLength;
        }

        /// <summary>
        /// TTS 응답의 오디오 길이를 바탕으로 처리 결과의 비용을 증가시킵니다.
        /// </summary>
        /// <remarks>
        /// ttsResult.AudioLength 값이 있으면(HasValue) 해당 길이를 0.1 단위로 올림하여 result.Cost에 더합니다.
        /// 예: AudioLength가 0.15이면 Math.Ceiling(0.15 / 0.1) = 2가 더해집니다.
        /// </remarks>
        /// <param name="result">비용을 누적할 ChatProcessResult 인스턴스.</param>
        /// <param name="ttsResult">오디오 길이 정보를 포함한 TextToSpeechResponse.</param>
        private void UpdateCost(ChatProcessResult result, TextToSpeechResponse ttsResult)
        {
            if (ttsResult.AudioLength.HasValue) {
                result.Cost += Math.Ceiling(ttsResult.AudioLength.Value / 0.1);
            }
        }

        /// <summary>
        /// 지정된 TTS 결과 배열을 집계하여 처리된(성공한) 세그먼트 수와 총 비용을 디버그 로그로 기록합니다.
        /// </summary>
        /// <param name="sessionId">로그에 포함할 세션 식별자.</param>
        /// <param name="ttsResults">세그먼트별 TTS 결과 배열; 각 항목은 세그먼트 인덱스와 TextToSpeechResponse를 포함합니다. 성공(Success == true)한 항목만 처리된 것으로 계산됩니다.</param>
        /// <param name="totalCost">전체 비용(누적된 비용 합계).</param>
        private void LogProcessingCompletion(string sessionId, (int idx, TextToSpeechResponse)[] ttsResults, double totalCost)
        {
            var processedCount = ttsResults.Count(r => r.Item2.Success);
            _logger.LogDebug("TTS 처리 완료: 세션 {SessionId}, 처리된 세그먼트 {ProcessedCount}개, 총 비용 {TotalCost}",
                sessionId, processedCount, totalCost);
        }

        /// <summary>
        /// 지정한 음성 프로파일과 감정으로 텍스트를 음성으로 변환하고 결과를 반환합니다.
        /// </summary>
        /// <param name="profile">사용할 음성 프로파일(언어, 음성 ID 등 설정을 포함).</param>
        /// <param name="text">변환할 텍스트(비어 있거나 길이 초과 시 유효성 검사에 의해 오류 응답이 생성됩니다).</param>
        /// <param name="emotion">요청할 음성 스타일/감정(프로파일에서 지원하지 않는 값은 처리 결과에 영향 가능).</param>
        /// <returns>
        /// TextToSpeech 클라이언트의 응답을 반환합니다. 입력 유효성 검사 실패나 처리 중 예외 발생 시에는 실패 상태의 TextToSpeechResponse(에러 메시지 포함)를 반환합니다.
        /// </returns>
        private async Task<TextToSpeechResponse> ProcessTTSAsync(VoiceProfile profile, string text, string emotion)
        {
            var startTime = DateTime.UtcNow;

            try {
                ValidateText(text);
                var request = CreateTTSRequest(profile, text, emotion);
                var response = await _ttsClient.TextToSpeechAsync(request);

                LogTTSProcessingTime(startTime, response.AudioLength);
                return response;
            }
            catch (ValidationException vex) {
                _logger.LogWarning(vex, "[TTS] 요청 검증 실패");
                return CreateErrorResponse(vex.Message);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "[TTS] TTS 서비스 오류 발생");
                return CreateErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// 주어진 음성 프로필, 텍스트, 감정 값을 사용해 TextToSpeechRequest 객체를 생성하여 반환합니다.
        /// </summary>
        /// <param name="profile">요청에 사용할 음성 프로필(언어 및 음성 식별자 포함).</param>
        /// <param name="text">합성할 텍스트 내용(사전 유효성 검사가 적용됨).</param>
        /// <param name="emotion">요청할 음성 스타일/감정(프로필의 지원 목록에 따라 정규화되어야 함).</param>
        /// <returns>생성된 TextToSpeechRequest 인스턴스.</returns>
        private TextToSpeechRequest CreateTTSRequest(VoiceProfile profile, string text, string emotion)
        {
            return new TextToSpeechRequest {
                Text = text,
                Language = profile.DefaultLanguage,
                Emotion = emotion,
                VoiceSettings = new VoiceSettings(),
                VoiceId = profile.VoiceId
            };
        }

        /// <summary>
        /// TTS 처리에 소요된 시간을 계산하여 로그로 기록합니다.
        /// </summary>
        /// <param name="startTime">TTS 요청 시작 시각(UTC). 이 값과 현재 시각의 차이로 처리 시간을 계산합니다.</param>
        /// <param name="audioLength">생성된 오디오의 길이(초). 값이 없을 수 있습니다.</param>
        private void LogTTSProcessingTime(DateTime startTime, double? audioLength)
        {
            var endTime = DateTime.UtcNow;
            var processingTime = (endTime - startTime).TotalMilliseconds;

            _logger.LogInformation("[TTS] 응답 생성 완료: 오디오 길이 ({AudioLength:F2}초), 요청 시간({ProcessingTimeMs:F2}ms)",
                audioLength, processingTime);
        }

        /// <summary>
        /// 오류 메시지를 담은 실패 상태의 TextToSpeechResponse 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="errorMessage">응답에 포함할 오류 설명 문자열.</param>
        /// <returns>Success가 false이고 ErrorMessage에 지정된 값을 가진 TextToSpeechResponse.</returns>
        private TextToSpeechResponse CreateErrorResponse(string errorMessage)
        {
            return new TextToSpeechResponse {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// 입력 텍스트가 TTS 처리 조건을 만족하는지 검증합니다.
        /// </summary>
        /// <param name="text">검증할 텍스트. 비어 있지 않아야 하며 최대 300자 이하여야 합니다.</param>
        /// <exception cref="ValidationException">다음 경우에 발생합니다:
        /// <list type="bullet">
        /// <item>텍스트가 null, 빈 문자열 또는 공백 문자만 있는 경우 (ErrorCode.MESSAGE_EMPTY)</item>
        /// <item>텍스트 길이가 300자를 초과하는 경우 (ErrorCode.MESSAGE_TOO_LONG)</item>
        /// </list>
        /// </exception>
        private void ValidateText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ValidationException(ErrorCode.MESSAGE_EMPTY, "텍스트가 비어있습니다.");
            if (text.Length > 300)
                throw new ValidationException(ErrorCode.MESSAGE_TOO_LONG, "텍스트는 300자를 초과할 수 없습니다.");
        }
    }
}

