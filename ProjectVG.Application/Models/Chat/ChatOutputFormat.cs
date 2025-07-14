using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectVG.Application.Models.Chat
{
    public class ChatOutputFormat
    {
        public string[] AllowedEmotions { get; }
        public string FormatBlockName { get; } = "응답";
        public string SummaryBlockName { get; } = "상황";
        public string EmotionBlockName { get; } = "감정";

        public ChatOutputFormat(string[] allowedEmotions)
        {
            AllowedEmotions = allowedEmotions ?? Array.Empty<string>();
        }

        public string GetInstructionBlock()
        {
            string emotionList = AllowedEmotions.Length > 0 ? string.Join(", ", AllowedEmotions) : "(감정 없음)";
            return $@"
[start]
{FormatBlockName}: (여기에 네 대답 작성)
{SummaryBlockName}: (지금 상황을 한두 줄로 요약)
{EmotionBlockName}: ({emotionList} 중 하나 골라서 작성)
[end]
";
        }

        public string GetFullInstructions(string baseInstructions)
        {
            return $"{baseInstructions}\n\n응답은 아래 형식만 써. 딴 포맷 쓰지 마:\n{GetInstructionBlock()}";
        }

        public ChatOutputFormatResult Parse(string llmText)
        {
            if (string.IsNullOrWhiteSpace(llmText))
                return new ChatOutputFormatResult();

            var startIdx = llmText.IndexOf("[start]");
            var endIdx = llmText.IndexOf("[end]", startIdx + 7);
            if (startIdx == -1 || endIdx == -1)
                return new ChatOutputFormatResult { Response = llmText.Trim() };

            var block = llmText.Substring(startIdx + 7, endIdx - (startIdx + 7)).Trim();
            string response = "", summary = "", emotion = "";
            foreach (var line in block.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith($"{FormatBlockName}:") || trimmed.StartsWith($"{FormatBlockName} :"))
                    response = trimmed.Substring(trimmed.IndexOf(":") + 1).Trim();
                else if (trimmed.StartsWith($"{SummaryBlockName}:") || trimmed.StartsWith($"{SummaryBlockName} :") || trimmed.StartsWith("요약:") || trimmed.StartsWith("요약 :"))
                    summary = trimmed.Substring(trimmed.IndexOf(":") + 1).Trim();
                else if (trimmed.StartsWith($"{EmotionBlockName}:") || trimmed.StartsWith($"{EmotionBlockName} :"))
                    emotion = trimmed.Substring(trimmed.IndexOf(":") + 1).Trim();
            }
            return new ChatOutputFormatResult
            {
                Response = response,
                Summary = summary,
                Emotion = emotion
            };
        }
    }

    public class ChatOutputFormatResult
    {
        public string Response { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Emotion { get; set; } = string.Empty;
    }
} 