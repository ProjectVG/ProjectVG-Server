using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ProjectVG.Common.Constants
{
    public static class ChatOutputFormat
    {
        public static string GetFormatInstructions()
        {
            string emotionList = string.Join(", ", EmotionConstants.SupportedEmotions);
            return $@"Reply ONLY in this format:
[emotion] text [emotion] text ...

Emotion must be one of: {emotionList}

# 예시
[neutral] 내가 그런다고 좋아할 것 같아? [shy] 하지만 츄 해준다면 좀 달라질지도...
";
        }

        public static ChatOutputFormatResult Parse(string llmText, string voiceName = null)
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
                emotions.Add("netural");
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

    public class ChatOutputFormatResult
    {
        public string Response { get; set; } = string.Empty;
        public List<string> Emotion { get; set; } = new List<string>();
        public List<string> Text { get; set; } = new List<string>();
    }
} 