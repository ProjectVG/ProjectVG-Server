using ProjectVG.Application.Models.Character;

namespace ProjectVG.Application.Models.Chat
{
    public class ProcessChatCommand
    {
        public Guid RequestId { get; }
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public Guid UserId { get; set; }
        public Guid CharacterId { get; set; }
        public DateTime RequestedAt { get; }
        public string? Action { get; set; }
        public string? Instruction { get; set; }
        public bool UseTTS { get; set; } = true;

        public CharacterDto? Character { get; private set; }

        /// <summary>
        /// 명령에 대한 캐릭터 데이터를 연결합니다.
        /// </summary>
        /// <param name="character">이 ProcessChatCommand에 설정할 CharacterDto 객체.</param>
        internal void SetCharacter(CharacterDto character)
        {
            Character = character;
        }

        public bool IsCharacterLoaded => Character != null;

        /// <summary>
        /// 빈 요청으로 ProcessChatCommand 인스턴스를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 생성 시 RequestId에 새 GUID를 할당하고 RequestedAt을 UTC 현재 시각으로 설정합니다.
        /// </remarks>
        public ProcessChatCommand()
        {
            RequestId = Guid.NewGuid();
            RequestedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 새 채팅 처리 요청을 생성합니다.
        /// </summary>
        /// <param name="userId">요청을 보낸 사용자 식별자.</param>
        /// <param name="characterId">명령에 연관된 캐릭터의 식별자.</param>
        /// <param name="message">사용자 메시지 내용.</param>
        /// <param name="sessionId">선택적 세션 식별자(기본값: 빈 문자열).</param>
        /// <remarks>
        /// 생성 시 RequestId는 새 GUID로 설정되며 RequestedAt은 UTC 현재 시각으로 초기화됩니다.
        /// </remarks>
        public ProcessChatCommand(Guid userId, Guid characterId, string message, string sessionId = "")
        {
            RequestId = Guid.NewGuid();
            UserId = userId;
            CharacterId = characterId;
            Message = message;
            SessionId = sessionId;
            RequestedAt = DateTime.UtcNow;
        }
    }
}
