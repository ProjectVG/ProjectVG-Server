using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ProjectVG.Application.Models.Character;
using ProjectVG.Common.Configuration;
using ProjectVG.Domain.Entities.ConversationHistorys;

namespace ProjectVG.Application.Models.Chat
{
    public class ChatRequestCommand
    {
        /// === 요청 정보 ===  
        public Guid Id { get; }
        public string UserPrompt { get; private set; } = string.Empty;
        public Guid UserId { get; private set; }
        public Guid CharacterId { get; private set; }
        public DateTime UserRequestAt { get; private set; }
        public DateTime ProcessAt { get; private set; }

        /// === 요청 옵션 ===
        public bool UseTTS { get; private set; } = true;

        /// == 내부 처리 정보 ===
        public IEnumerable<ConversationHistory>? ConversationHistory { get; private set; } = new List<ConversationHistory>();
        public string UserIntent { get; private set; } = string.Empty;
        public UserInputProcessType ProcessType { get; private set; } = UserInputProcessType.Undefined;
        public double Cost { get; private set; }

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

        public void SetConversationHistory(IEnumerable<ConversationHistory> histories)
        {
            ConversationHistory = histories;
        }

        public void SetAnalysisResult(UserInputProcessType processType, string userIntent)
        {
            ProcessType = processType;
            UserIntent = userIntent;
        }

        public void AddCost(double value)
        {
            Cost += value;
        }

    }
}
