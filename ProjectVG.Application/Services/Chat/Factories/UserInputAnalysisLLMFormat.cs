using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat.Factories
{
    public class UserInputAnalysisLLMFormat : ILLMFormat<string, (UserIntentType ProcessType, string Intent)>
    {
        public string Model => LLMModelInfo.GPT4oMini.Name;
        public float Temperature => 0.3f;
        public int MaxTokens => 300;

        public string GetSystemMessage(string? input)
        {
            return @"Analyze user input and extract: (1) PROCESS_TYPE: 0=chat, 1=ignore, (2) INTENT: one-sentence summary in Korean. Focus on simple, clear intent recognition.";
        }

        public string GetInstructions(string? input)
        {
            return @"Output format:
PROCESS_TYPE: [0|1]
INTENT: [Korean sentence]

Rules: 0=normal chat/questions, 1=meaningless/invalid

Examples:
Input: ""한달전에 구매한 킥보드 생각나나?""
PROCESS_TYPE: 0
INTENT: 과거 경험에 대한 회상 질문

Input: ""as .d101""
PROCESS_TYPE: 1  
INTENT: 해석불가";
        }


        public (UserIntentType ProcessType, string Intent) Parse(string llmResponse, string input)
        {
            try {
                var lines = llmResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var response = new Dictionary<string, string>();

                // 각 라인을 파싱하여 키-값 쌍으로 저장 (값 내 콜론 허용)
                foreach (var line in lines) {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine)) continue;

                    var colonIndex = trimmedLine.IndexOf(':');
                    if (colonIndex > 0 && colonIndex < trimmedLine.Length - 1) {
                        var key = trimmedLine.Substring(0, colonIndex).Trim();
                        var value = trimmedLine.Substring(colonIndex + 1).Trim();

                        if (key == "PROCESS_TYPE" || key == "INTENT") {
                            response[key] = value;
                        }
                    }
                }

                if (!response.TryGetValue("PROCESS_TYPE", out var processTypeStr) ||
                    !int.TryParse(processTypeStr, out var processTypeValue)) {
                    return (UserIntentType.Chat, "일반적인 대화");
                }

                var processType = (UserIntentType)processTypeValue;
                var intent = response.GetValueOrDefault("INTENT", "일반적인 대화");

                return (processType, intent);
            }
            catch (Exception) {
                return (UserIntentType.Chat, "일반적인 대화");
            }
        }

        public double CalculateCost(int promptTokens, int completionTokens)
        {
            return LLMModelInfo.CalculateCost(Model, promptTokens, completionTokens);
        }

    }
}
