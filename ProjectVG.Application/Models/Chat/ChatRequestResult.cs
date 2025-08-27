namespace ProjectVG.Application.Models.Chat
{
    public class ChatRequestResult
    {
        public bool IsAccepted { get; private set; }
        public string Status { get; private set; } = string.Empty;
        public string Message { get; private set; } = string.Empty;
        public string ErrorCode { get; private set; } = string.Empty;
        public DateTime RequestedAt { get; private set; }
        public string SessionId { get; private set; } = string.Empty;
        public Guid UserId { get; private set; }
        public Guid CharacterId { get; private set; }

        public static ChatRequestResult Accepted(string sessionId, Guid userId, Guid characterId)
        {
            return new ChatRequestResult
            {
                IsAccepted = true,
                Status = "ACCEPTED",
                Message = "채팅 요청이 성공적으로 수락되었습니다. 처리 중입니다.",
                RequestedAt = DateTime.UtcNow,
                SessionId = sessionId,
                UserId = userId,
                CharacterId = characterId
            };
        }

        public static ChatRequestResult Rejected(string message, string errorCode = "VALIDATION_ERROR", string sessionId = "", Guid userId = default, Guid characterId = default)
        {
            return new ChatRequestResult
            {
                IsAccepted = false,
                Status = "REJECTED",
                Message = message,
                ErrorCode = errorCode,
                RequestedAt = DateTime.UtcNow,
                SessionId = sessionId,
                UserId = userId,
                CharacterId = characterId
            };
        }
    }
}
