using ProjectVG.Application.Models.Chat;
using ProjectVG.Common.Constants;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectVG.Application.Services.Chat.Factories
{
    public class ChatLLMFormat : ILLMFormat<ChatProcessContext, List<ChatMessageSegment>>
    {
        public ChatLLMFormat()
        {
        }

        public string GetSystemMessage(ChatProcessContext input)
        {
            var character = input.Character ?? throw new InvalidOperationException("캐릭터 정보가 로드되지 않았습니다.");
            
            var sb = new StringBuilder();
            sb.AppendLine($"당신은 {character.Name}입니다.");
            sb.AppendLine($"설명: {character.Description}");
            sb.AppendLine($"역할: {character.Role}");
            sb.AppendLine($"성격: {character.Personality}");
            sb.AppendLine($"말투: {character.SpeechStyle}");
            
            return sb.ToString();
        }

        public string GetInstructions(ChatProcessContext input)
        {
            var sb = new StringBuilder();
            
            if (input.MemoryContext?.Any() == true)
            {
                sb.AppendLine("관련 기억:");
                foreach (var memory in input.MemoryContext)
                {
                    sb.AppendLine($"- {memory}");
                }
                sb.AppendLine();
            }
            
            var conversationHistory = input.ParseConversationHistory(5);
            if (conversationHistory.Any())
            {
                sb.AppendLine("최근 대화 기록:");
                foreach (var history in conversationHistory)
                {
                    sb.AppendLine($"- {history}");
                }
                sb.AppendLine();
            }
            
            sb.AppendLine(GetFormatInstructions());
            
            return sb.ToString();
        }

        public string Model => LLMModelInfo.GPT4oMini.Name;
        public float Temperature => 0.7f;
        public int MaxTokens => 1000;

        public List<ChatMessageSegment> Parse(string llmResponse, ChatProcessContext input)
        {
            return ParseChatResponseToSegments(llmResponse, input.Character?.VoiceId);
        }

        public double CalculateCost(int promptTokens, int completionTokens)
        {
            return LLMModelInfo.CalculateCost(Model, promptTokens, completionTokens);
        }

        private string GetFormatInstructions()
        {
            string emotionList = string.Join(", ", EmotionConstants.SupportedEmotions);
            return $@"Reply ONLY in this format:
[emotion] text [emotion] text ...

Emotion must be one of: {emotionList}

# 예시
[neutral] 내가 그런다고 좋아할 것 같아? [shy] 하지만 츄 해준다면 좀 달라질지도...
";
        }

        private List<ChatMessageSegment> ParseChatResponseToSegments(string llmText, string? voiceId = null)
        {
            if (string.IsNullOrWhiteSpace(llmText))
                return new List<ChatMessageSegment>();

            string response = llmText.Trim();
            var segments = new List<ChatMessageSegment>();
            var seenTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var matches = Regex.Matches(response, @"\[(.*?)\]\s*([^\[]+)");
            var emotionMap = GetEmotionMap(voiceId);

            if (matches.Count > 0)
            {
                ProcessMatches(matches, emotionMap, segments, seenTexts);
            }
            else
            {
                var segment = ChatMessageSegment.CreateTextOnly(response, 0);
                segment.Emotion = "neutral";
                segments.Add(segment);
            }

            return segments;
        }

        private Dictionary<string, string>? GetEmotionMap(string? voiceId)
        {
            if (string.IsNullOrWhiteSpace(voiceId))
                return null;

            var profile = VoiceCatalog.GetProfileById(voiceId);
            return profile?.EmotionMap;
        }

        private void ProcessMatches(MatchCollection matches, Dictionary<string, string>? emotionMap, List<ChatMessageSegment> segments, HashSet<string> seenTexts)
        {
            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                if (match.Groups.Count >= 3)
                {
                    var originalEmotion = match.Groups[1].Value.Trim();
                    var mappedEmotion = emotionMap != null && emotionMap.ContainsKey(originalEmotion)
                        ? emotionMap[originalEmotion]
                        : originalEmotion;
                    var text = match.Groups[2].Value.Trim();
                    
                    if (!seenTexts.Contains(text))
                    {
                        seenTexts.Add(text);
                        var segment = ChatMessageSegment.CreateTextOnly(text, segments.Count);
                        segment.Emotion = mappedEmotion;
                        segments.Add(segment);
                    }
                }
            }
        }
    }
}
