namespace ProjectVG.Application.Models.Chat
{
    public class ChatRequestResponse
    {
        public bool IsAccepted { get; private set; }
        public string Status { get; private set; } = string.Empty;
        public string Message { get; private set; } = string.Empty;
        public string ErrorCode { get; private set; } = string.Empty;
        public DateTime RequestedAt { get; private set; }
        public string SessionId { get; private set; } = string.Empty;
        public Guid UserId { get; private set; }
        public Guid CharacterId { get; private set; }

        /// <summary>
        /// 채팅 요청을 수락한 결과를 담은 ChatRequestResponse 인스턴스를 생성하여 반환합니다.
        /// </summary>
        /// <param name="sessionId">연결된 세션의 식별자.</param>
        /// <param name="userId">요청한 사용자(유저)의 식별자.</param>
        /// <param name="characterId">대화에 사용될 캐릭터의 식별자.</param>
        /// <returns>IsAccepted가 true이고 Status가 "ACCEPTED"로 설정된 ChatRequestResponse (RequestedAt은 UTC 현재 시간).</returns>
        public static ChatRequestResponse Accepted(string sessionId, Guid userId, Guid characterId)
        {
            return new ChatRequestResponse
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

        /// <summary>
        /// 거부된 채팅 요청 응답을 생성합니다.
        /// </summary>
        /// <param name="message">거부 사유를 설명하는 인간 친화적 메시지.</param>
        /// <param name="errorCode">거부 원인을 나타내는 오류 코드(기본값: "VALIDATION_ERROR").</param>
        /// <param name="sessionId">관련 세션 식별자(지정하지 않으면 빈 문자열).</param>
        /// <param name="userId">요청을 보낸 사용자 식별자(기본값: Guid.Empty).</param>
        /// <param name="characterId">관련 캐릭터 식별자(기본값: Guid.Empty).</param>
        /// <returns>
        /// IsAccepted가 false이고 Status가 "REJECTED"로 설정된 새로운 <see cref="ChatRequestResponse"/> 인스턴스.
        /// RequestedAt은 UTC 기준 현재 시각으로 설정됩니다.
        /// </returns>
        public static ChatRequestResponse Rejected(string message, string errorCode = "VALIDATION_ERROR", string sessionId = "", Guid userId = default, Guid characterId = default)
        {
            return new ChatRequestResponse
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
