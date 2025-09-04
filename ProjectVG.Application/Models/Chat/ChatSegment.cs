using System.Collections.Generic;

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



        public bool HasContent => !string.IsNullOrEmpty(Content);
        public bool HasAudio => AudioData != null && AudioData.Length > 0;
        public bool IsEmpty => !HasContent && !HasActions;
        public bool HasEmotion => !string.IsNullOrEmpty(Emotion);
        public bool HasActions => Actions != null && Actions.Any();
        


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
    }
}
