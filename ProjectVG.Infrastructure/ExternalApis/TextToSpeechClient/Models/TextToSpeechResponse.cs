using System.Text.Json.Serialization;

namespace ProjectVG.Infrastructure.ExternalApis.TextToSpeech.Models
{
    public class TextToSpeechResponse
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
        /// 오디오 데이터 (바이트 배열)
        /// </summary>
        [JsonIgnore]
        public byte[]? AudioData { get; set; }

        /// <summary>
        /// 오디오 길이 (초)
        /// </summary>
        [JsonIgnore]
        public float? AudioLength { get; set; }

        /// <summary>
        /// 오디오 형식 (audio/wav, audio/mpeg 등)
        /// </summary>
        [JsonIgnore]
        public string? ContentType { get; set; }

        /// <summary>
        /// HTTP 상태 코드
        /// </summary>
        [JsonIgnore]
        public int StatusCode { get; set; } = 200;
    }
} 