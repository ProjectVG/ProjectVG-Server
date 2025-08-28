using ProjectVG.Application.Models.Character;
using ProjectVG.Domain.Entities.ConversationHistorys;

namespace ProjectVG.Application.Models.Chat
{
    public class ChatProcessContext
    {
        public string SessionId { get; private set; } = string.Empty;
        public Guid UserId { get; private set; }
        public Guid CharacterId { get; private set; }
        public string UserMessage { get; private set; } = string.Empty;
        public string MemoryStore { get; private set; } = string.Empty;
        public bool UseTTS { get; private set; } = true;
        
        public CharacterDto? Character { get; private set; }
        public IEnumerable<string>? MemoryContext { get; private set; }
        public IEnumerable<ConversationHistory>? ConversationHistory { get; private set; }
        
        public string Response { get; private set; } = string.Empty;
        public double Cost { get; private set; }
        public List<ChatMessageSegment> Segments { get; private set; } = new List<ChatMessageSegment>();
        
        public string FullText => string.Join(" ", Segments.Where(s => s.HasText).Select(s => s.Text));
        public bool HasAudio => Segments.Any(s => s.HasAudio);
        public bool HasText => Segments.Any(s => s.HasText);


        /// <summary>
        /// 주어진 ProcessChatCommand에서 세션, 사용자, 캐릭터 식별자와 메시지, 메모리 키 및 TTS 사용 여부를 초기화하여 ChatProcessContext 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="command">초기화에 사용할 입력 명령(세션Id, UserId, CharacterId, Message, UseTTS 포함).</param>
        public ChatProcessContext(ProcessChatCommand command)
        {
            SessionId = command.SessionId;
            UserId = command.UserId;
            CharacterId = command.CharacterId;
            UserMessage = command.Message;
            MemoryStore = command.UserId.ToString();
            UseTTS = command.UseTTS;
        }

        /// <summary>
        /// 주어진 입력으로 ChatProcessContext 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="command">세션, 사용자, 캐릭터 식별자와 메시지 및 UseTTS 설정을 포함한 처리 명령.</param>
        /// <param name="character">대화에 사용할 캐릭터 데이터(없을 수 있음).</param>
        /// <param name="conversationHistory">대화 기록 항목 시퀀스(없을 수 있음).</param>
        /// <param name="memoryContext">메모리 컨텍스트 항목 시퀀스(없을 수 있음).</param>
        /// <remarks>
        /// 생성자는 Command에서 SessionId, UserId, CharacterId, Message, UseTTS 값을 복사하고 MemoryStore는 UserId의 문자열 표현으로 설정합니다.
        /// 제공된 캐릭터, 대화 기록, 메모리 컨텍스트는 각각 대응하는 프로퍼티에 할당됩니다.
        /// </remarks>
        public ChatProcessContext(
            ProcessChatCommand command,
            CharacterDto character,
            IEnumerable<ConversationHistory> conversationHistory,
            IEnumerable<string> memoryContext)
        {
            SessionId = command.SessionId;
            UserId = command.UserId;
            CharacterId = command.CharacterId;
            UserMessage = command.Message;
            MemoryStore = command.UserId.ToString();
            UseTTS = command.UseTTS;
            
            Character = character;
            ConversationHistory = conversationHistory;
            MemoryContext = memoryContext;
        }

        /// <summary>
        /// 처리된 응답 텍스트와 응답을 구성하는 세그먼트 목록 및 해당 비용을 현재 컨텍스트에 설정합니다.
        /// </summary>
        /// <param name="response">생성된 전체 응답 텍스트(읽기/표시용).</param>
        /// <param name="segments">응답을 구성하는 텍스트/오디오 세그먼트의 리스트.</param>
        /// <param name="cost">이 응답에 할당된 총 비용.</param>
        public void SetResponse(string response, List<ChatMessageSegment> segments, double cost)
        {
            Response = response;
            Segments = segments;
            Cost = cost;
        }

        /// <summary>
        /// 누적 비용에 지정한 값을 더합니다.
        /// </summary>
        /// <param name="additionalCost">더할 비용(음수일 경우 비용을 감소시킵니다).</param>
        public void AddCost(double additionalCost)
        {
            Cost += additionalCost;
        }

        /// <summary>
        /// 대화 기록에서 최근 항목을 지정된 개수만큼 추출해 "Role: Content" 형식의 문자열 시퀀스로 반환합니다.
        /// </summary>
        /// <param name="count">반환할 최대 항목 수(기본값: 5). ConversationHistory의 항목 수보다 크면 가능한 만큼 반환합니다.</param>
        /// <returns>
        /// 각 항목을 "Role: Content"로 포맷한 문자열의 열거. ConversationHistory가 null이면 빈 열거를 반환합니다.
        /// </returns>
        public IEnumerable<string> ParseConversationHistory(int count = 5)
        {
            if (ConversationHistory == null) return Enumerable.Empty<string>();

            return ConversationHistory
                .Take(count)
                .Select(h => $"{h.Role}: {h.Content}");
        }
    }
}
