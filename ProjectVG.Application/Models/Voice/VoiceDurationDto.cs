using ProjectVG.Infrastructure.ExternalApis.TextToSpeech.Models;

namespace ProjectVG.Application.Models.Voice
{
    /// <summary>
    /// 음성 지속 시간 예측 데이터 전송 객체
    /// </summary>
    public class VoiceDurationDto
    {
        /// <summary>
        /// 예측 ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 세션 ID
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// 보이스 ID
        /// </summary>
        public string VoiceId { get; set; } = string.Empty;

        /// <summary>
        /// 원본 텍스트
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 언어 설정
        /// </summary>
        public string Language { get; set; } = "ko";

        /// <summary>
        /// 감정 스타일
        /// </summary>
        public string? Style { get; set; }

        /// <summary>
        /// 음성 설정
        /// </summary>
        public VoiceSettings? VoiceSettings { get; set; }

        /// <summary>
        /// 예측된 음성 지속 시간 (초)
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public VoiceDurationDto()
        {
        }

        /// <summary>
        /// VoiceDurationResponse로부터 DTO를 생성하는 생성자
        /// </summary>
        /// <param name="response">TextToSpeechDurationResponse</param>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="voiceId">보이스 ID</param>
        /// <param name="text">원본 텍스트</param>
        /// <param name="language">언어</param>
        /// <param name="style">스타일</param>
        /// <param name="voiceSettings">음성 설정</param>
        public VoiceDurationDto(TextToSpeechDurationResponse response, string sessionId, string voiceId, string text, string language, string? style, VoiceSettings? voiceSettings)
        {
            Id = Guid.NewGuid();
            SessionId = sessionId;
            VoiceId = voiceId;
            Text = text;
            Language = language;
            Style = style;
            VoiceSettings = voiceSettings;
            Duration = response.Duration;
            CreatedAt = DateTime.UtcNow;
        }
    }
} 