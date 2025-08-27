using ProjectVG.Application.Models.Character;

namespace ProjectVG.Application.Models.Chat
{
    public class ChatRequestCommand
    {
        /// === 요청 정보 ===  
        public Guid Id { get; }
        public string UserPrompt { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public Guid CharacterId { get; set; }
        public DateTime UserRequestAt { get; set; }
        public DateTime ProcessAt { get; private set; }

        /// == 내부 처리 정보 ===

        /// === 요청 옵션 ===
        public bool UseTTS { get; set; } = true;

        public ChatRequestCommand()
        {
            Id = Guid.NewGuid();
            ProcessAt = DateTime.UtcNow;
        }

        public ChatRequestCommand(Guid userId, Guid characterId, string userPrompt, DateTime requestedAt)
        {
            Id = Guid.NewGuid();
            UserId = userId;
            CharacterId = characterId;
            UserPrompt = userPrompt;
            UserRequestAt = requestedAt;
            ProcessAt = DateTime.UtcNow;
        }
    }
}
