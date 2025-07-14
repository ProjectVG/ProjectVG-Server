using System.Text.Json.Serialization;

namespace ProjectVG.Infrastructure.ExternalApis.TextToSpeech.Models
{
    public class TextToSpeechDurationResponse
    {
        /// <summary>
        /// 요청 성공 여부
        /// </summary>
        [JsonIgnore]
        public bool Success { get; set; } = true;

        /// <summary>
        /// 오류 메시지
        /// </summary>
        [JsonIgnore]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 예측된 음성 지속 시간 (초)
        /// </summary>
        [JsonPropertyName("duration")]
        public float Duration { get; set; }

        /// <summary>
        /// HTTP 상태 코드
        /// </summary>
        [JsonIgnore]
        public int StatusCode { get; set; } = 200;
    }
} 