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

        // LOH 방지를 위한 ArrayPool 기반 메모리 관리
        internal IMemoryOwner<byte>? AudioMemoryOwner { get; private set; }
        internal int AudioDataSize { get; private set; }
        private bool _disposed;



        public bool HasContent => !string.IsNullOrEmpty(Content);
        public bool HasAudio => (AudioData != null && AudioData.Length > 0) || (AudioMemoryOwner != null && AudioDataSize > 0);
        public bool IsEmpty => !HasContent && !HasActions;
        public bool HasEmotion => !string.IsNullOrEmpty(Emotion);
        public bool HasActions => Actions != null && Actions.Any();

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

        // 주의: 원본 인스턴스의 AudioMemoryOwner 해제됨
        public ChatSegment WithAudioMemory(IMemoryOwner<byte> audioMemoryOwner, int audioDataSize, string audioContentType, float audioLength)
        {
            if (audioMemoryOwner is null)
                throw new ArgumentNullException(nameof(audioMemoryOwner));

            if (audioDataSize < 0 || audioDataSize > audioMemoryOwner.Memory.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(audioDataSize),
                    audioDataSize,
                    $"audioDataSize는 0 이상 {audioMemoryOwner.Memory.Length} 이하여야 합니다.");

            // 기존 소유자 해제 및 상태 정리
            this.AudioMemoryOwner?.Dispose();
            this.AudioMemoryOwner = null;
            this.AudioDataSize = 0;

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
                AudioData = null
            };
        }

        /// <summary>
        /// 오디오 메모리를 부착한 새 인스턴스 생성 (원본 불변)
        /// </summary>
        public ChatSegment AttachAudioMemory(IMemoryOwner<byte> audioMemoryOwner, int audioDataSize, string audioContentType, float audioLength)
        {
            if (audioMemoryOwner is null)
                throw new ArgumentNullException(nameof(audioMemoryOwner));

            if (audioDataSize < 0 || audioDataSize > audioMemoryOwner.Memory.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(audioDataSize),
                    audioDataSize,
                    $"audioDataSize는 0 이상 {audioMemoryOwner.Memory.Length} 이하여야 합니다.");

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
                AudioData = null
            };
        }

        // 필요시만 사용 - LOH 위험 있음
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

        public void Dispose()
        {
            if (_disposed) return;
            AudioMemoryOwner?.Dispose();
            _disposed = true;
        }
    }
}
