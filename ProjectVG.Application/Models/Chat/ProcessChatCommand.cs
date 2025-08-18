using ProjectVG.Application.Models.Character;

namespace ProjectVG.Application.Models.Chat
{
    public class ProcessChatCommand
    {
        private string _requestId = Guid.NewGuid().ToString();
        public string RequestId
        {
            get => string.IsNullOrEmpty(_requestId) ? (_requestId = Guid.NewGuid().ToString()) : _requestId;
            set => _requestId = value;
        }
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public Guid UserId { get; set; }
        public Guid CharacterId { get; set; }
        public DateTime RequestedAt { get; set; }
        public string? Action { get; set; }
        public string? Instruction { get; set; }
        public bool UseTTS { get; set; } = true;

        public CharacterDto? Character { get; private set; }

        internal void SetCharacter(CharacterDto character)
        {
            Character = character;
        }

        public bool IsCharacterLoaded => Character != null;
    }
}
