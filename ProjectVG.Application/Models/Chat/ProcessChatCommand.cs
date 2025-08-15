using ProjectVG.Application.Models.Character;

namespace ProjectVG.Application.Models.Chat
{
    /// <summary>
    /// 채팅 처리 명령 (내부 비즈니스 로직용)
    /// </summary>
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

        /// <summary>
        /// 캐릭터 정보 (지연 로딩)
        /// </summary>
        public CharacterDto? Character { get; private set; }

        /// <summary>
        /// 캐릭터 정보 설정 (내부에서만 사용)
        /// </summary>
        internal void SetCharacter(CharacterDto character)
        {
            Character = character;
        }

        /// <summary>
        /// 캐릭터 정보가 로드되었는지 확인
        /// </summary>
        public bool IsCharacterLoaded => Character != null;
    }
} 