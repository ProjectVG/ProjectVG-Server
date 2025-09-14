using System.Collections.Generic;
using System.Buffers;

namespace ProjectVG.Application.Models.Chat
{
    public sealed class ChatSegment : IDisposable
    {

        public string Content { get; private set; } = string.Empty;

        public int Order { get; private set; }

        public string? Emotion { get; private set; }

        public List<string>? Actions { get; private set; }

        public byte[]? AudioData { get; private set; }
        public string? AudioContentType { get; private set; }
        public float? AudioLength { get; private set; }

        // 스트림 기반 음성 데이터 처리를 위한 새로운 프로퍼티
        internal IMemoryOwner<byte>? AudioMemoryOwner { get; private set; }
        internal int AudioDataSize { get; private set; }

        // Dispose 멱등성 보장을 위한 플래그
        private bool _disposed;



        public bool HasContent => !string.IsNullOrEmpty(Content);
        public bool HasAudio => (AudioData != null && AudioData.Length > 0) || (AudioMemoryOwner != null && AudioDataSize > 0);
        public bool IsEmpty => !HasContent && !HasActions;
        public bool HasEmotion => !string.IsNullOrEmpty(Emotion);
        public bool HasActions => Actions != null && Actions.Any();

        /// <summary>
        /// 메모리 효율적인 방식으로 음성 데이터에 접근합니다
        /// </summary>
        public ReadOnlySpan<byte> GetAudioSpan()
        {
            if (AudioMemoryOwner != null && AudioDataSize > 0)
            {
                var memory = AudioMemoryOwner.Memory;
                var safeSize = Math.Min(AudioDataSize, memory.Length);
                return memory.Span.Slice(0, safeSize);
            }
            if (AudioData != null)
            {
                return new ReadOnlySpan<byte>(AudioData);
            }
            return ReadOnlySpan<byte>.Empty;
        }
        


        private ChatSegment() { }

        public static ChatSegment Create(string content, string? emotion = null, List<string>? actions = null, int order = 0)
        {
            return new ChatSegment
            {
                Content = content,
                Emotion = emotion,
                Actions = actions,
                Order = order
            };
        }

        public static ChatSegment CreateText(string content, string? emotion = null, int order = 0)
        {
            return Create(content, emotion, null, order);
        }

        public static ChatSegment CreateAction(string action, int order = 0)
        {
            return Create("", null, new List<string> { action }, order);
        }

        // Method to add audio data (returns new record instance)
        public ChatSegment WithAudioData(byte[] audioData, string audioContentType, float audioLength)
        {
            return new ChatSegment
            {
                Content = this.Content,
                Order = this.Order,
                Emotion = this.Emotion,
                Actions = this.Actions,
                AudioData = audioData,
                AudioContentType = audioContentType,
                AudioLength = audioLength
            };
        }

        /// <summary>
        /// 메모리 효율적인 방식으로 음성 데이터를 추가합니다 (LOH 방지)
        /// 소유권이 이전되므로 호출자는 더 이상 audioMemoryOwner를 해제하지 않아야 합니다.
        /// </summary>
        /// <param name="audioMemoryOwner">소유권이 이전될 메모리 소유자</param>
        /// <param name="audioDataSize">실제 오디오 데이터 크기 (메모리 크기 이하여야 함)</param>
        /// <param name="audioContentType">오디오 컨텐츠 타입</param>
        /// <param name="audioLength">오디오 길이 (초)</param>
        /// <returns>새로운 ChatSegment 인스턴스</returns>
        /// <exception cref="ArgumentNullException">audioMemoryOwner가 null인 경우</exception>
        /// <exception cref="ArgumentOutOfRangeException">audioDataSize가 유효하지 않은 경우</exception>
        public ChatSegment WithAudioMemory(IMemoryOwner<byte> audioMemoryOwner, int audioDataSize, string audioContentType, float audioLength)
        {
            if (audioMemoryOwner is null)
                throw new ArgumentNullException(nameof(audioMemoryOwner));

            if (audioDataSize < 0 || audioDataSize > audioMemoryOwner.Memory.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(audioDataSize),
                    audioDataSize,
                    $"audioDataSize는 0 이상 {audioMemoryOwner.Memory.Length} 이하여야 합니다.");

            // 기존 AudioMemoryOwner가 있다면 해제 (소유권 이전)
            this.AudioMemoryOwner?.Dispose();

            return new ChatSegment
            {
                Content = this.Content,
                Order = this.Order,
                Emotion = this.Emotion,
                Actions = this.Actions,
                AudioMemoryOwner = audioMemoryOwner,
                AudioDataSize = audioDataSize,
                AudioContentType = audioContentType,
                AudioLength = audioLength,
                // 기존 AudioData는 null로 설정하여 중복 저장 방지
                AudioData = null
            };
        }

        /// <summary>
        /// 음성 데이터를 배열로 변환합니다 (필요한 경우에만 사용)
        /// </summary>
        public byte[]? GetAudioDataAsArray()
        {
            if (AudioData != null)
            {
                return AudioData;
            }

            if (AudioMemoryOwner != null && AudioDataSize > 0)
            {
                var span = AudioMemoryOwner.Memory.Span.Slice(0, AudioDataSize);
                return span.ToArray();
            }

            return null;
        }

        /// <summary>
        /// 리소스 해제 (IMemoryOwner 해제)
        /// 멱등성을 보장하여 여러 번 호출해도 안전합니다.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            // 관리형 리소스만 해제 (IMemoryOwner)
            AudioMemoryOwner?.Dispose();

            _disposed = true;
            // 파이널라이저가 없으므로 GC.SuppressFinalize 불필요
        }
    }
}
