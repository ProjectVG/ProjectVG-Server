using ProjectVG.Infrastructure.Integrations.TextToSpeechClient.Models;

namespace ProjectVG.Application.Models.Voice
{
    /// <summary>
    /// 음성 데이터 전송 객체 (내부 비즈니스 로직용)
    /// </summary>
    public class VoiceDto
    {
        /// <summary>
        /// 음성 ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 세션 ID
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

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
        /// 오디오 데이터 (바이트 배열)
        /// </summary>
        public byte[]? AudioData { get; set; }

        /// <summary>
        /// 오디오 길이 (초)
        /// </summary>
        public float? AudioLength { get; set; }

        /// <summary>
        /// 오디오 형식
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public VoiceDto()
        {
        }

        /// <summary>
        /// VoiceResponse로부터 DTO를 생성하는 생성자
        /// </summary>
        /// <param name="response">TextToSpeechResponse</param>
        /// <param name="sessionId">세션 ID</param>
        /// <param name="text">원본 텍스트</param>
        /// <param name="language">언어</param>
        /// <param name="style">스타일</param>
        /// <param name="voiceSettings">음성 설정</param>
        public VoiceDto(TextToSpeechResponse response, string sessionId, string text, string language, string? style, VoiceSettings? voiceSettings)
        {
            Id = Guid.NewGuid();
            SessionId = sessionId;
            Text = text;
            Language = language;
            Style = style;
            VoiceSettings = voiceSettings;
            AudioData = response.AudioData;
            AudioLength = response.AudioLength;
            ContentType = response.ContentType;
            CreatedAt = DateTime.UtcNow;
        }
    }
} 