using ProjectVG.Application.Models.Chat;
using ProjectVG.Common.Constants;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectVG.Application.Services.Chat.Factories
{
    public class ChatLLMFormat : ILLMFormat<ChatPreprocessContext, ChatOutputFormatResult>
    {
        /// <summary>
        /// ChatLLMFormat의 기본 생성자입니다.
        /// </summary>
        /// <remarks>
        /// 매개변수 없이 인스턴스를 초기화하며 부수 효과가 없습니다.
        /// </remarks>
        public ChatLLMFormat()
        {
        }

        /// <summary>
        /// 입력된 전처리 컨텍스트의 캐릭터 정보를 바탕으로 시스템 메시지(프롬프트)를 생성합니다.
        /// </summary>
        /// <param name="input">캐릭터 정보가 포함된 채팅 전처리 컨텍스트. <see cref="ChatPreprocessContext.Character"/>가 필요합니다.</param>
        /// <returns>
        /// 캐릭터의 이름, 설명, 역할, 성격, 말투를 각 줄로 구성한 시스템 메시지 문자열.
        /// 예: "당신은 {Name}입니다.\n설명: {Description}\n역할: {Role}\n성격: {Personality}\n말투: {SpeechStyle}\n"
        /// </returns>
        /// <exception cref="InvalidOperationException">input.Character가 null인 경우 발생합니다. 메시지: "캐릭터 정보가 로드되지 않았습니다."</exception>
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

        /// <summary>
        /// LLM에 전달할 보조 지침 문자열을 조합합니다.
        /// </summary>
        /// <remarks>
        /// 입력 컨텍스트의 메모리 항목이 있으면 "관련 기억:" 섹션을, 최근 대화 기록(최대 5건)이 있으면 "최근 대화 기록:" 섹션을 추가하고,
        /// 마지막으로 LLM 응답 형식 지침(GetFormatInstructions)을 붙여 반환합니다.
        /// </remarks>
        /// <param name="input">메모리 컨텍스트와 대화 기록을 제공하는 전처리 컨텍스트. MemoryContext 항목과 ParseConversationHistory(5) 결과를 사용합니다.</param>
        /// <returns>메모리, 최근 대화 기록(있을 경우), 및 출력 포맷 지침을 포함한 문자열.</returns>
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

        /// <summary>
        /// LLM 응답 문자열을 파싱하여 구조화된 ChatOutputFormatResult로 반환합니다.
        /// </summary>
        /// <remarks>
        /// 내부적으로 ParseChatResponse를 호출하며, 입력 컨텍스트의 VoiceName을 사용해 감정 매핑을 적용합니다.
        /// 빈 또는 공백 응답은 비어있는 결과로 변환됩니다.
        /// </remarks>
        /// <param name="llmResponse">LLM이 반환한 원시 응답 텍스트.</param>
        /// <param name="input">파싱 시 사용할 컨텍스트(예: VoiceName을 통한 감정 매핑).</param>
        /// <returns>파싱된 응답을 담은 ChatOutputFormatResult 객체.</returns>
        public ChatOutputFormatResult Parse(string llmResponse, ChatPreprocessContext input)
        {
            return ParseChatResponse(llmResponse, input.VoiceName);
        }

        /// <summary>
        /// 주어진 토큰 소모량을 기반으로 해당 모델의 입력/출력 비용을 합산해 추정 비용을 계산합니다.
        /// </summary>
        /// <param name="tokensUsed">계산에 사용할 총 토큰 수(입력 + 출력). 음수가 아닌 정수여야 합니다.</param>
        /// <returns>LLMModelInfo에 정의된 모델별 입력/출력 비용(백만 토큰 단위)을 적용해 계산한 추정 비용(double).</returns>
        public double CalculateCost(int tokensUsed)
        {
            var inputCost = LLMModelInfo.GetInputCost(Model);
            var outputCost = LLMModelInfo.GetOutputCost(Model);
            
            // 토큰 수를 백만 단위로 변환하여 비용 계산
            return (tokensUsed / 1_000_000.0) * (inputCost + outputCost);
        }

        /// <summary>
        /// LLM에게 응답 형식을 강제하는 지침 문자열을 생성합니다.
        /// </summary>
        /// <returns>
        /// LLM이 반드시 따라야 하는 출력 형식(예: <c>[emotion] text [emotion] text ...</c>)과 허용된 감정 목록 및 예시를 포함한 지침 문자열을 반환합니다.
        /// </returns>
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

        /// <summary>
        /// LLM 응답 텍스트를 파싱하여 감정(emotion)과 대응 텍스트 조각들을 추출한 결과를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 입력 문자열에서 "[감정] 텍스트" 패턴을 정규식 @"\[(.*?)\]\s*([^\[]+)" 로 탐지합니다.
        /// - 패턴이 하나 이상 발견되면 각 매치의 첫 그룹을 감정으로, 두번째 그룹을 텍스트로 수집합니다.
        /// - 패턴이 없으면 전체 응답을 단일 텍스트로 취급하고 감정은 "neutral"로 설정합니다.
        /// 제공된 voiceName에 대해 VoiceCatalog에서 프로필을 조회할 수 있고, 프로필에 EmotionMap이 있으면
        /// 원래 감정을 해당 맵을 사용해 매핑합니다.
        /// </remarks>
        /// <param name="llmText">파싱할 LLM 응답 문자열(공백 또는 null이면 빈 결과 반환).</param>
        /// <param name="voiceName">선택적 음성 프로필 이름. 매핑 가능한 감정 변환이 있으면 적용된다.</param>
        /// <returns>
        /// ChatOutputFormatResult:
        /// - Response: 원본(트림된) 응답 문자열.
        /// - Emotion: 파싱 및(있다면) 매핑된 감정 목록(각 텍스트 조각과 인덱스 일치).
        /// - Text: 각 감정에 대응하는 텍스트 조각들의 목록.
        /// </returns>
        private ChatOutputFormatResult ParseChatResponse(string llmText, string? voiceName = null)
        {
            if (string.IsNullOrWhiteSpace(llmText))
                return new ChatOutputFormatResult();

            string response = llmText.Trim();
            var emotions = new List<string>();
            var texts = new List<string>();

            // [감정] 답변 패턴 추출
            var matches = Regex.Matches(response, @"\[(.*?)\]\s*([^\[]+)");
            
            // 보이스별 감정 매핑
            Dictionary<string, string>? emotionMap = null;
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
