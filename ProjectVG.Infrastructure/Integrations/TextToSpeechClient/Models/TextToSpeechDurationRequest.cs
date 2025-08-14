using System.Text.Json.Serialization;

namespace ProjectVG.Infrastructure.Integrations.TextToSpeechClient.Models
{
    public class TextToSpeechDurationRequest
    {
        /// <summary>
        /// 보이스 ID
        /// </summary>
        [JsonIgnore]
        public string VoiceId { get; set; } = "";

        /// <summary>
        /// 텍스트를 음성으로 변환할 내용 (최대 300자)
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        /// <summary>
        /// 텍스트 언어. 음성에서 지원하는 언어(ko, en, ja) 중에서 선택
        /// </summary>
        [JsonPropertyName("language")]
        public string Language { get; set; } = "ko";

        /// <summary>
        /// 적용할 감정 스타일(중립, 행복 등). 입력하지 않으면 기본 스타일이 사용됩니다
        /// </summary>
        [JsonPropertyName("style")]
        public string? Style { get; set; }

        /// <summary>
        /// 사용할 음성 모델(sona_speech_1). 생략 시 자동 적용
        /// </summary>
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        /// <summary>
        /// 음성 피치, 음조, 속도를 조정하는 고급 옵션
        /// </summary>
        [JsonPropertyName("voice_settings")]
        public VoiceSettings? VoiceSettings { get; set; }
    }
} 