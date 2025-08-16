using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat
{
    public interface IChatService
    {
        /// <summary>
        /// 채팅 요청을 검증하고 처리합니다
        /// </summary>
        /// <param name="command">채팅 처리 명령</param>
        /// <returns>작업 요청 결과</returns>
        Task<ChatRequestResponse> EnqueueChatRequestAsync(ProcessChatCommand command);
    }
} 