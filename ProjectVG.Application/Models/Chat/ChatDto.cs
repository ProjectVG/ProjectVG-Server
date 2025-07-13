using ProjectVG.Domain.Entities.Chat;

namespace ProjectVG.Application.Models.Chat
{
    /// <summary>
    /// 채팅 데이터 전송 객체 (내부 비즈니스 로직용)
    /// </summary>
    public class ChatDto
    {
        public Guid Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Action { get; set; }
        public Guid? CharacterId { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public ChatDto()
        {
        }

        /// <summary>
        /// Chat 엔티티로부터 DTO를 생성하는 생성자
        /// </summary>
        /// <param name="chat">Chat 엔티티</param>
        public ChatDto(ChatMessage chat)
        {
            Id = chat.Id;
            SessionId = chat.SessionId;
            Actor = chat.Actor;
            Message = chat.Message;
            Action = chat.Action;
            CharacterId = chat.CharacterId;
            CreatedAt = chat.CreatedAt;
        }

        /// <summary>
        /// DTO를 Chat 엔티티로 변환
        /// </summary>
        /// <returns>Chat 엔티티</returns>
        public ChatMessage ToChatMessage()
        {
            return new ChatMessage
            {
                Id = Id,
                SessionId = SessionId,
                Actor = Actor,
                Message = Message,
                Action = Action,
                CharacterId = CharacterId,
                CreatedAt = CreatedAt
            };
        }
    }
} 