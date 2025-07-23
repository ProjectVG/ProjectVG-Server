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
            string emotionList = AllowedEmotions.Length > 0 ? string.Join(",", AllowedEmotions) : "none";
            return $@"응답은 반드시 다음 JSON 형식으로만 작성하세요:
응답 형식:
{{
""response"": ""[캐릭터의 개성과 말투를 반영한 응답]"",
""summary"": ""상황 요약"",
""emotion"": ""{emotionList} 중 하나""
}}";
        }

        public string GetFullInstructions(string baseInstructions)
        {
            return $"{baseInstructions}\n\n{GetInstructionBlock()}";
        }

        public ChatOutputFormatResult Parse(string llmText)
        {
            if (string.IsNullOrWhiteSpace(llmText))
                return new ChatOutputFormatResult();

            string jsonCandidate = llmText.Trim();

            // JSON 객체만 추출 (앞뒤에 의미없는 문장 제거)
            var match = Regex.Match(jsonCandidate, @"\{[\s\S]*\}");
            if (match.Success)
            {
                jsonCandidate = match.Value;
            }

            try
            {
                // JSON 형식 파싱 시도
                var jsonDoc = JsonDocument.Parse(jsonCandidate);
                var root = jsonDoc.RootElement;

                return new ChatOutputFormatResult
                {
                    Response = root.TryGetProperty("response", out var r) ? r.GetString() ?? "" : "",
                    Summary = root.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
                    Emotion = root.TryGetProperty("emotion", out var e) ? e.GetString() ?? "" : ""
                };
            }
            catch (JsonException)
            {
                // JSON 파싱 실패 시 기존 텍스트를 응답으로 사용
                return new ChatOutputFormatResult { Response = llmText.Trim() };
            }
        }
    }

    public class ChatOutputFormatResult
    {
        public string Response { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Emotion { get; set; } = string.Empty;
    }
} 