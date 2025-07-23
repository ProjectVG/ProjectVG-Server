using System.Text.Json.Serialization;

namespace ProjectVG.Infrastructure.ExternalApis.TextToSpeech.Models
{
    public class TextToSpeechRequest
    {
        [JsonIgnore]
        public string VoiceId { get; set; }

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
        public string? Emotion { get; set; }

        /// <summary>
        /// 사용할 음성 모델(sona_speech_1). 생략 시 자동 적용
        /// </summary>
        [Obsolete("이 필드는 더 이상 사용하지 않습니다")]
        [JsonPropertyName("model")]
        public string? Model { get; set; } 

        /// <summary>
        /// 음성 피치, 음조, 속도를 조정하는 고급 옵션
        /// </summary>
        [JsonPropertyName("voice_settings")]
        public VoiceSettings? VoiceSettings { get; set; }
    }

    public class VoiceSettings
    {
        /// <summary>
        /// 피치 레벨을 조절합니다. 0은 원래 음성 피치이며, ±12단계가 가능합니다. 1단계는 반음입니다.
        /// </summary>
        [JsonPropertyName("pitch_shift")]
        public int PitchShift { get; set; } = 0;

        /// <summary>
        /// 음성 중 음조 변화의 정도를 조절합니다. 값이 작을수록 음조가 평탄해지고, 값이 클수록 음조가 풍부해집니다.
        /// </summary>
        [JsonPropertyName("pitch_variance")]
        public float PitchVariance { get; set; } = 1.0f;

        /// <summary>
        /// 음성 속도를 조절합니다. 값이 1보다 작으면 음성 속도가 느려지고, 값이 1보다 크면 음성 속도가 빨라집니다.
        /// </summary>
        [JsonPropertyName("speed")]
        public float Speed { get; set; } = 1.1f;
    }
} 