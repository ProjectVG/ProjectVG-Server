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

        internal void SetCharacter(CharacterDto character)
        {
            Character = character;
        }

        public bool IsCharacterLoaded => Character != null;

        // 기본 생성자
        public ProcessChatCommand()
        {
            RequestId = Guid.NewGuid();
            RequestedAt = DateTime.UtcNow;
        }

        // 주요 값들을 받는 생성자
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
