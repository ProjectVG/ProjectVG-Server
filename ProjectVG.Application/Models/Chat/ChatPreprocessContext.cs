using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Character;
using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Domain.Enums;

namespace ProjectVG.Application.Models.Chat
{
    public class ChatPreprocessContext
    {
        public string SessionId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public Guid CharacterId { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        
        public string? Action { get; set; }
        public List<string> MemoryContext { get; set; } = new();
        public IEnumerable<ConversationHistory> ConversationHistory { get; set; } = new List<ConversationHistory>();

        public string MemoryStore { get; set; } = string.Empty;
        public string VoiceName { get; set; } = string.Empty;
        public CharacterDto? Character { get; set; }

        /// <summary>
        /// ProcessChatCommand의 값을 사용해 ChatPreprocessContext 인스턴스를 초기화합니다.
        /// </summary>
        /// <remarks>
        /// - command의 SessionId, UserId, CharacterId, Message, Action을 각각 매핑합니다.
        /// - MemoryStore는 command.UserId.ToString()으로 설정됩니다.
        /// - memoryContext 또는 conversationHistory가 null이면 빈 목록으로 초기화합니다.
        /// - command.Character의 값을 Character에 할당하고, VoiceName은 Character.VoiceId로 설정합니다.
        /// </remarks>
        /// <param name="command">초기화에 사용할 입력 명령. Character는 null이 아니어야 합니다.</param>
        /// <param name="memoryContext">초기 MemoryContext 목록(없으면 새 빈 리스트로 대체).</param>
        /// <param name="conversationHistory">초기 ConversationHistory 열거(없으면 새 빈 리스트로 대체).</param>
        public ChatPreprocessContext(
            ProcessChatCommand command,
            List<string> memoryContext,
            IEnumerable<ConversationHistory> conversationHistory)
        {
            SessionId = command.SessionId;
            UserId = command.UserId;
            CharacterId = command.CharacterId;
            UserMessage = command.Message;
            MemoryStore = command.UserId.ToString();
            Action = command.Action;
            MemoryContext = memoryContext ?? new List<string>();
            ConversationHistory = conversationHistory ?? new List<ConversationHistory>();
            
            Character = command.Character!;
            VoiceName = command.Character!.VoiceId;
        }

        /// <summary>
        /// ConversationHistory의 항목들을 "Role: Content" 형식의 문자열 목록으로 변환하여 반환합니다.
        /// </summary>
        /// <returns>ConversationHistory가 존재하면 각 항목을 "Role: Content"로 매핑한 문자열 목록을 반환하고, 없으면 빈 목록을 반환합니다.</returns>
        public List<string> ParseConversationHistory()
        {
            return ConversationHistory?.Select(c => $"{c.Role}: {c.Content}").ToList() ?? new List<string>();
        }

        /// <summary>
        /// 대화 기록에서 각 항목을 "Role: Content" 형식의 문자열로 변환하여 최대 <paramref name="takeCount"/>개까지 반환합니다.
        /// </summary>
        /// <param name="takeCount">가져올 항목 수(음수나 0이면 빈 리스트를 반환합니다).</param>
        /// <returns>변환된 문자열 목록(ConversationHistory가 null이면 빈 목록).</returns>
        public List<string> ParseConversationHistory(int takeCount)
        {
            return ConversationHistory?.Take(takeCount).Select(c => $"{c.Role}: {c.Content}").ToList() ?? new List<string>();
        }

        /// <summary>
        /// 현재 컨텍스트의 핵심 필드를 한 줄로 요약한 문자열을 반환합니다.
        /// </summary>
        /// <returns>SessionId, UserId, CharacterId, UserMessage, MemoryContext 항목 수와 ConversationHistory 항목 수를 포함한 한 줄 요약 문자열.</returns>
        public override string ToString()
        {
            return $"ChatPreprocessContext(SessionId={SessionId}, UserId={UserId}, CharacterId={CharacterId}, UserMessage='{UserMessage}', MemoryContext.Count={MemoryContext.Count}, ConversationHistory.Count={ConversationHistory.Count()})";
        }

        /// <summary>
        — ChatPreprocessContext의 상세 정보를 사람이 읽기 쉬운 멀티라인 문자열로 구성하여 반환합니다.
        /// </summary>
        /// <remarks>
        /// 반환된 문자열에는 세션 및 식별자(SessionId, UserId, CharacterId), 액션(Action, null이면 "N/A"로 표기),
        /// 음성 이름(VoiceName), 메모리 저장소(MemoryStore), 사용자 메시지(UserMessage), 캐릭터 이름(없으면 "Not Loaded")과
        /// MemoryContext 항목 목록 및 ParseConversationHistory()로 생성된 대화 기록 항목들이 인덱스와 함께 포함됩니다.
        /// </remarks>
        /// <returns>ChatPreprocessContext의 필드와 컬렉션 내용을 사람이 읽기 쉬운 형식으로 담은 문자열.</returns>
        public string GetDetailedInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== ChatPreprocessContext ===");
            info.AppendLine($"SessionId: {SessionId}");
            info.AppendLine($"UserId: {UserId}");
            info.AppendLine($"CharacterId: {CharacterId}");
            info.AppendLine($"Action: {Action ?? "N/A"}");
            info.AppendLine($"VoiceName: {VoiceName}");
            info.AppendLine($"MemoryStore: {MemoryStore}");
            info.AppendLine($"UserMessage: {UserMessage}");
            info.AppendLine($"Character: {(Character != null ? Character.Name : "Not Loaded")}");
            
            info.AppendLine($"MemoryContext ({MemoryContext.Count} items):");
            for (int i = 0; i < MemoryContext.Count; i++)
            {
                info.AppendLine($"  [{i}]: {MemoryContext[i]}");
            }
            
            info.AppendLine($"ConversationHistory ({ConversationHistory.Count()} items):");
            var parsedHistory = ParseConversationHistory();
            for (int i = 0; i < parsedHistory.Count; i++)
            {
                info.AppendLine($"  [{i}]: {parsedHistory[i]}");
            }
            info.AppendLine("=== End ChatPreprocessContext ===");
            
            return info.ToString();
        }
    }
}
