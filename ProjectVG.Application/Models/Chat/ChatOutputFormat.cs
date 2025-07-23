using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProjectVG.Application.Models.Chat
{
    public class ChatOutputFormat
    {
        public string[] AllowedEmotions { get; }

        public ChatOutputFormat(string[] allowedEmotions)
        {
            AllowedEmotions = allowedEmotions ?? Array.Empty<string>();
        }

        public string GetInstructionBlock()
        {
            string emotionList = AllowedEmotions.Length > 0 ? string.Join(", ", AllowedEmotions) : "자유롭게 감정을 작성";
            return $@"응답은 반드시 다음 형식으로만 작성하세요:
[감정] 답변 [감정] 답변 ...

감정은 {emotionList} 중 하나여야 합니다.

예시:
[neutral] 오늘 점심 맛있게 먹었어?
[neutral] 내가 그런다고 좋아 할것 같아? [shy] 하지만 츄 해준다면 좀 달라질지도...";
        }

        public string GetFullInstructions(string baseInstructions)
        {
            return $"{baseInstructions}\n\n{GetInstructionBlock()}";
        }

        public ChatOutputFormatResult Parse(string llmText)
        {
            if (string.IsNullOrWhiteSpace(llmText))
                return new ChatOutputFormatResult();

            string response = llmText.Trim();
            var emotions = new List<string>();
            var texts = new List<string>();

            // [감정] 답변 패턴 추출
            var matches = Regex.Matches(response, @"\[(.*?)\]\s*([^\[]+)");
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        emotions.Add(match.Groups[1].Value.Trim());
                        texts.Add(match.Groups[2].Value.Trim());
                    }
                }
            }
            else
            {
                emotions.Add("unknown");
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