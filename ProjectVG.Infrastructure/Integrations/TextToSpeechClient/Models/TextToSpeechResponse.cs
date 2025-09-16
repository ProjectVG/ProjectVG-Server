using System.Text.Json.Serialization;
using System.Buffers;

namespace ProjectVG.Infrastructure.Integrations.TextToSpeechClient.Models
{
    /// <summary>
    /// TTS API 응답 모델 - IMemoryOwner 기반 메모리 관리
    /// </summary>
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
        /// 주의: ChatSegment로 이전하지 않을 경우 직접 Dispose() 필요
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

        /// <summary>
        /// 오디오 메모리 소유권을 안전하게 가져갑니다
        /// </summary>
        public bool TryTakeAudioOwner(out IMemoryOwner<byte>? owner, out int size)
        {
            owner = AudioMemoryOwner;
            size = AudioDataSize;

            // 소유권 이전 후 현재 객체에서 제거하여 중복 해제 방지
            AudioMemoryOwner = null;
            AudioDataSize = 0;

            return owner != null;
        }
    }
} 