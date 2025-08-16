using ProjectVG.Application.Models.Chat;
using ProjectVG.Common.Constants;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectVG.Application.Services.Chat.Factories
{
    public class ChatLLMFormat : ILLMFormat<ChatPreprocessContext, ChatOutputFormatResult>
    {
        public ChatLLMFormat()
        {
        }

        public string GetSystemMessage(ChatPreprocessContext input)
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

        public string GetInstructions(ChatPreprocessContext input)
        {
            var sb = new StringBuilder();
            
            // 메모리 컨텍스트 추가
            if (input.MemoryContext?.Any() == true)
            {
                sb.AppendLine("관련 기억:");
                foreach (var memory in input.MemoryContext)
                {
                    sb.AppendLine($"- {memory}");
                }
                sb.AppendLine();
            }
            
            // 대화 기록 추가
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
            
            // 출력 포맷 지침 추가
            sb.AppendLine(GetFormatInstructions());
            
            return sb.ToString();
        }

        public string Model => LLMModelInfo.GPT4oMini.Name;
        public float Temperature => 0.7f;
        public int MaxTokens => 1000;

        public ChatOutputFormatResult Parse(string llmResponse, ChatPreprocessContext input)
        {
            return ParseChatResponse(llmResponse, input.VoiceName);
        }

        public double CalculateCost(int tokensUsed)
        {
            var inputCost = LLMModelInfo.GetInputCost(Model);
            var outputCost = LLMModelInfo.GetOutputCost(Model);
            
            // 토큰 수를 백만 단위로 변환하여 비용 계산
            return (tokensUsed / 1_000_000.0) * (inputCost + outputCost);
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

        private ChatOutputFormatResult ParseChatResponse(string llmText, string voiceName = null)
        {
            if (string.IsNullOrWhiteSpace(llmText))
                return new ChatOutputFormatResult();

            string response = llmText.Trim();
            var emotions = new List<string>();
            var texts = new List<string>();

            // [감정] 답변 패턴 추출
            var matches = Regex.Matches(response, @"\[(.*?)\]\s*([^\[]+)");
            
            // 보이스별 감정 매핑
            Dictionary<string, string> emotionMap = null;
            if (!string.IsNullOrWhiteSpace(voiceName))
            {
                var profile = VoiceCatalog.GetProfile(voiceName);
                if (profile != null && profile.EmotionMap != null)
                    emotionMap = profile.EmotionMap;
            }

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var originalEmotion = match.Groups[1].Value.Trim();
                        var mappedEmotion = emotionMap != null && emotionMap.ContainsKey(originalEmotion)
                            ? emotionMap[originalEmotion]
                            : originalEmotion;
                        emotions.Add(mappedEmotion);
                        texts.Add(match.Groups[2].Value.Trim());
                    }
                }
            }
            else
            {
                emotions.Add("neutral");
                texts.Add(response);
            }

            return new ChatOutputFormatResult
            {
                Response = response,
                Emotion = emotions,
                Text = texts
            };
        }
    }
}
