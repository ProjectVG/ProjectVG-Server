using ProjectVG.Application.Models.Chat;
using ProjectVG.Common.Constants;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectVG.Application.Services.Chat.Factories
{
    public class ChatLLMFormat : ILLMFormat<ChatProcessContext, List<ChatMessageSegment>>
    {
        /// <summary>
        /// ChatLLMFormat의 기본 생성자입니다.
        /// 새 인스턴스를 초기 상태로 초기화합니다.
        /// </summary>
        public ChatLLMFormat()
        {
        }

        /// <summary>
        /// 주어진 처리 컨텍스트에서 캐릭터 정보를 읽어 시스템 메시지(캐릭터 프로필 문자열)를 생성합니다.
        /// </summary>
        /// <param name="input">캐릭터 정보가 포함된 처리 컨텍스트. <see cref="ChatProcessContext.Character"/>가 null이면 예외가 발생합니다.</param>
        /// <returns>캐릭터 이름, 설명, 역할, 성격, 말투를 각각 새 줄로 나열한 문자열.</returns>
        /// <exception cref="InvalidOperationException">input.Character가 null인 경우 발생합니다 ("캐릭터 정보가 로드되지 않았습니다.").</exception>
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

        /// <summary>
        /// 시스템/LLM 지시문을 조합하여 반환합니다.
        /// </summary>
        /// <param name="input">메모리(관련 기억)와 대화 기록을 포함한 처리 컨텍스트. MemoryContext와 ParseConversationHistory(5)의 결과를 사용합니다.</param>
        /// <returns>메모리 목록(있을 경우), 최근 대화 기록(최대 5건, 있으면)과 포맷 지시문을 순서대로 포함한 지시문 문자열.</returns>
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

        /// <summary>
        /// LLM의 텍스트 응답을 ChatMessageSegment 목록으로 변환합니다.
        /// </summary>
        /// <param name="llmResponse">LLM이 생성한 원문 응답 텍스트.</param>
        /// <param name="input">파싱 시 사용되는 컨텍스트(특히 캐릭터의 VoiceId를 통해 감정 매핑을 결정).</param>
        /// <returns>응답에서 추출한 ChatMessageSegment 목록. 입력이 비어있거나 파싱 가능한 태그가 없으면 단일 'neutral' 세그먼트 또는 빈 목록을 반환할 수 있습니다.</returns>
        public List<ChatMessageSegment> Parse(string llmResponse, ChatProcessContext input)
        {
            return ParseChatResponseToSegments(llmResponse, input.Character?.VoiceId);
        }

        /// <summary>
        /// 지정한 프롬프트 토큰 수와 완료 토큰 수를 기반으로 모델 사용 비용을 계산합니다.
        /// </summary>
        /// <param name="promptTokens">프롬프트(입력) 토큰 수.</param>
        /// <param name="completionTokens">완료(출력) 토큰 수.</param>
        /// <returns>계산된 비용 (통화 단위: USD).</returns>
        public double CalculateCost(int promptTokens, int completionTokens)
        {
            return LLMModelInfo.CalculateCost(Model, promptTokens, completionTokens);
        }

        /// <summary>
        /// LLM에 반환 형식을 명시하는 지침 문자열을 생성합니다.
        /// </summary>
        /// <remarks>
        /// 결과 문자열은 응답이 반드시 감정 태그와 텍스트 쌍의 반복 형태로만 반환되도록 요구하며,
        /// 사용 가능한 감정 목록(EmotionConstants.SupportedEmotions)에 기반한 허용 감정을 포함하고 예시를 제공합니다.
        /// </remarks>
        /// <returns>LLM에게 전달할 포맷 지침을 담은 문자열.</returns>
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
        /// LLM 응답 텍스트를 ChatMessageSegment 목록으로 파싱합니다.
        /// </summary>
        /// <remarks>
        /// 입력 텍스트에서 "[감정] 발화" 형식의 태그들을 추출해 각 태그별로 ChatMessageSegment를 생성합니다.
        /// voiceId가 주어지면 음성 프로필의 감정 매핑을 적용하고, 동일한 발화 텍스트는 대소문자 무시 기준으로 중복 제거합니다.
        /// 입력이 비어있으면 빈 목록을 반환하며, 태그가 전혀 없으면 전체 응답을 단일 'neutral' 감정의 세그먼트로 반환합니다.
        /// </remarks>
        /// <param name="llmText">파싱할 LLM 응답 문자열.</param>
        /// <param name="voiceId">(선택) 감정 매핑을 적용할 음성 프로필 ID.</param>
        /// <returns>파싱된 ChatMessageSegment 목록. 입력이 비어있으면 빈 목록을 반환.</returns>
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

        /// <summary>
        /// 음성 프로필 ID에서 감정 매핑을 조회합니다.
        /// </summary>
        /// <param name="voiceId">조회할 음성 프로필의 ID. null 또는 공백이면 조회하지 않습니다.</param>
        /// <returns>프로필에 정의된 감정 매핑(Dictionary&lt;string,string&gt;). 프로필이 없거나 매핑이 없으면 null을 반환합니다.</returns>
        private Dictionary<string, string>? GetEmotionMap(string? voiceId)
        {
            if (string.IsNullOrWhiteSpace(voiceId))
                return null;

            var profile = VoiceCatalog.GetProfileById(voiceId);
            return profile?.EmotionMap;
        }

        /// <summary>
        /// 정규식 매치 컬렉션을 처리하여 중복되지 않는 텍스트 세그먼트를 생성하고 감정 태그를 적용해 segments에 추가합니다.
        /// </summary>
        /// <remarks>
        /// 각 매치는 그룹 1을 감정 식별자(예: "happy"), 그룹 2를 텍스트로 취급합니다. emotionMap이 제공되면 그룹 1의 값은 매핑을 통해 변환되며, 없으면 원본 감정을 사용합니다.
        /// 이미 seenTexts에 포함된 텍스트는 건너뛰어 중복 세그먼트 생성을 방지합니다. 새 세그먼트는 segments.Count를 인덱스로 하여 ChatMessageSegment.CreateTextOnly로 생성한 뒤 Emotion을 설정하고 segments에 추가됩니다.
        /// </remarks>
        /// <param name="matches">정규식으로 추출된 매치 컬렉션(그룹 1: 감정, 그룹 2: 텍스트를 기대).</param>
        /// <param name="emotionMap">원본 감정을 출력 감정으로 매핑하는 사전(없을 수 있음).</param>
        /// <param name="segments">생성된 ChatMessageSegment를 추가할 리스트(출력).</param>
        /// <param name="seenTexts">이미 추가된 텍스트를 추적하는 집합(중복 방지용).</param>
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
