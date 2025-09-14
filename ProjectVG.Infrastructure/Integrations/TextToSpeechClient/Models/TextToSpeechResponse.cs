using System.Text.Json.Serialization;
using System.Buffers;

namespace ProjectVG.Infrastructure.Integrations.TextToSpeechClient.Models
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
        /// 오디오 데이터 (바이트 배열) - 레거시 호환성용
        /// </summary>
        [JsonIgnore]
        public byte[]? AudioData { get; set; }

        /// <summary>
        /// ArrayPool 기반 오디오 메모리 소유자 (LOH 방지)
        /// </summary>
        [JsonIgnore]
        public IMemoryOwner<byte>? AudioMemoryOwner { get; set; }

        /// <summary>
        /// 실제 오디오 데이터 크기
        /// </summary>
        [JsonIgnore]
        public int AudioDataSize { get; set; }

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