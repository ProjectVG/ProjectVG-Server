using ProjectVG.Application.Models.Chat;
using ProjectVG.Common.Constants;
using System.Text.Json;

namespace ProjectVG.Application.Services.Chat.Factories
{
    public class UserInputAnalysisLLMFormat : ILLMFormat<string, UserInputAnalysis>
    {
        public string GetSystemMessage(string input)
        {
            return @"당신은 사용자 입력을 분석하는 AI입니다. 
사용자의 입력을 분석하여 의도, 유효성, 키워드 등을 추출해야 합니다.
JSON 형식으로만 응답하세요.";
        }

        public string GetInstructions(string input)
        {
            return @"다음 JSON 형식으로 응답하세요:

{
  ""intent"": ""사용자의 의도 (예: 질문, 대화, 명령 등)"",
  ""isValid"": true/false,
  ""rejectionReason"": ""거부 사유 (isValid가 false인 경우)"",
  ""action"": ""수행할 액션 (proceed, reject, clarify 등)"",
  ""keywords"": [""키워드1"", ""키워드2""],
  ""enhancedQuery"": ""향상된 검색 쿼리"",
  ""contextTime"": ""컨텍스트 시간 (YYYY-MM-DD HH:mm:ss 또는 null)""
}

분석 기준:
- 의미 없는 문자나 공격적인 내용은 거부
- 일반적인 대화나 질문은 허용
- 키워드는 핵심 개념만 추출
- 향상된 쿼리는 검색에 최적화된 형태로 변환";
        }

        public string Model => LLMModelInfo.GPT4oMini.Name;
        public float Temperature => 0.1f;
        public int MaxTokens => 500;

        public UserInputAnalysis Parse(string llmResponse, string input)
        {
            try
            {
                // JSON 응답 파싱
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(llmResponse);
                
                var intent = jsonResponse.GetProperty("intent").GetString() ?? "대화";
                var action = jsonResponse.GetProperty("action").GetString() ?? "proceed";
                var isValid = jsonResponse.GetProperty("isValid").GetBoolean();
                var rejectionReason = jsonResponse.TryGetProperty("rejectionReason", out var rejectProp) ? rejectProp.GetString() : null;
                var enhancedQuery = jsonResponse.TryGetProperty("enhancedQuery", out var queryProp) ? queryProp.GetString() : null;
                
                // 키워드 파싱
                var keywords = new List<string>();
                if (jsonResponse.TryGetProperty("keywords", out var keywordsProp) && keywordsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var keyword in keywordsProp.EnumerateArray())
                    {
                        if (keyword.ValueKind == JsonValueKind.String)
                        {
                            keywords.Add(keyword.GetString()!);
                        }
                    }
                }
                
                // 컨텍스트 시간 파싱
                DateTime? contextTime = null;
                if (jsonResponse.TryGetProperty("contextTime", out var timeProp) && timeProp.ValueKind == JsonValueKind.String)
                {
                    var timeStr = timeProp.GetString();
                    if (!string.IsNullOrEmpty(timeStr) && timeStr != "null")
                    {
                        if (DateTime.TryParse(timeStr, out var parsedTime))
                        {
                            contextTime = parsedTime;
                        }
                    }
                }

                if (isValid)
                {
                    return UserInputAnalysis.Valid(intent, contextTime, keywords, action, enhancedQuery);
                }
                else
                {
                    return UserInputAnalysis.Invalid(rejectionReason ?? "부적절한 입력", action);
                }
            }
            catch (Exception ex)
            {
                // 파싱 실패 시 기본값 반환
                return UserInputAnalysis.Valid("대화", null, new List<string>(), "proceed");
            }
        }

        public double CalculateCost(int tokensUsed)
        {
            var inputCost = LLMModelInfo.GetInputCost(Model);
            var outputCost = LLMModelInfo.GetOutputCost(Model);
            
            // 토큰 수를 백만 단위로 변환하여 비용 계산
            return (tokensUsed / 1_000_000.0) * (inputCost + outputCost);
        }
    }
}
