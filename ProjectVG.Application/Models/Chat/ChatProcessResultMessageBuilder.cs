using System.Text.Json.Serialization;

namespace ProjectVG.Application.Models.Chat
{
    public class ChatProcessResultMessageBuilder
    {
        private string _type = "chat";
        private string _messageType = "json";
        private string? _text;
        private string? _audioData;
        private string? _audioFormat;
        private float? _audioLength;
        private DateTime _timestamp = DateTime.UtcNow;
        private Dictionary<string, object>? _metadata;

        public ChatProcessResultMessageBuilder SetType(string type)
        {
            _type = type ?? "chat";
            return this;
        }

        public ChatProcessResultMessageBuilder SetMessageType(string messageType)
        {
            _messageType = messageType ?? "json";
            return this;
        }

        public ChatProcessResultMessageBuilder SetText(string? text)
        {
            _text = text;
            return this;
        }

        public ChatProcessResultMessageBuilder SetAudioData(byte[]? audioBytes)
        {
            if (audioBytes != null && audioBytes.Length > 0)
            {
                _audioData = Convert.ToBase64String(audioBytes);
            }
            else
            {
                _audioData = null;
            }
            return this;
        }

        public ChatProcessResultMessageBuilder SetAudioFormat(string? audioFormat)
        {
            _audioFormat = audioFormat;
            return this;
        }

        public ChatProcessResultMessageBuilder SetAudioLength(float? audioLength)
        {
            _audioLength = audioLength;
            return this;
        }

        public ChatProcessResultMessageBuilder SetTimestamp(DateTime timestamp)
        {
            _timestamp = timestamp;
            return this;
        }

        public ChatProcessResultMessageBuilder AddMetadata(string key, object value)
        {
            _metadata ??= new Dictionary<string, object>();
            _metadata[key] = value;
            return this;
        }

        public ChatProcessResultMessageBuilder SetMetadata(Dictionary<string, object>? metadata)
        {
            _metadata = metadata;
            return this;
        }

        public ChatProcessResultMessage Build()
        {
            return new ChatProcessResultMessage
            {
                Type = _type,
                MessageType = _messageType,
                Text = _text,
                AudioData = _audioData,
                AudioFormat = _audioFormat,
                AudioLength = _audioLength,
                Timestamp = _timestamp,
                Metadata = _metadata
            };
        }

        public static ChatProcessResultMessageBuilder FromSegment(ChatSegment segment)
        {
            var builder = new ChatProcessResultMessageBuilder()
                .SetType(segment.Type == SegmentType.Text ? "chat" : "action")
                .SetText(segment.Content)
                .SetTimestamp(DateTime.UtcNow);

            if (segment.Type == SegmentType.Text && segment.HasAudio)
            {
                builder
                    .SetAudioData(segment.AudioData)
                    .SetAudioFormat(segment.AudioContentType ?? "wav")
                    .SetAudioLength(segment.AudioLength);
            }

            if (segment.HasEmotion)
            {
                builder.AddMetadata("emotion", segment.Emotion!);
            }

            builder.AddMetadata("order", segment.Order);

            return builder;
        }

        public static ChatProcessResultMessage CreateFromSegment(ChatSegment segment)
        {
            return FromSegment(segment).Build();
        }
    }
}