using System.Collections.Generic;
using System.Buffers;

namespace ProjectVG.Application.Models.Chat
{
    public record ChatSegment
    {

        public string Content { get; init; } = string.Empty;

        public int Order { get; init; }

        public string? Emotion { get; init; }

        public List<string>? Actions { get; init; }

        public byte[]? AudioData { get; init; }
        public string? AudioContentType { get; init; }
        public float? AudioLength { get; init; }

        // 스트림 기반 음성 데이터 처리를 위한 새로운 프로퍼티
        public IMemoryOwner<byte>? AudioMemoryOwner { get; init; }
        public int AudioDataSize { get; init; }



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
            return this with
            {
                AudioData = audioData,
                AudioContentType = audioContentType,
                AudioLength = audioLength
            };
        }

        /// <summary>
        /// 메모리 효율적인 방식으로 음성 데이터를 추가합니다 (LOH 방지)
        /// </summary>
        public ChatSegment WithAudioMemory(IMemoryOwner<byte> audioMemoryOwner, int audioDataSize, string audioContentType, float audioLength)
        {
            return this with
            {
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
        /// </summary>
        public void Dispose()
        {
            AudioMemoryOwner?.Dispose();
        }
    }
}
