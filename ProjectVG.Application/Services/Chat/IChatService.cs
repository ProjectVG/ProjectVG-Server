using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat
{
    public interface IChatService
    {
        /// <summary>
        /// 채팅 요청을 검증하고 처리합니다
        /// </summary>
        /// <param name="command">채팅 처리 명령</param>
        /// <summary>
/// 채팅 처리 작업을 큐에 등록하고 비동기적으로 처리 결과를 반환합니다.
/// </summary>
/// <param name="command">처리할 채팅 요청을 나타내는 명령 객체(메시지, 메타데이터 등).</param>
/// <returns>작업 요청 결과를 포함하는 비동기 작업(Task&lt;ChatRequestResponse&gt;).</returns>
        Task<ChatRequestResponse> EnqueueChatRequestAsync(ProcessChatCommand command);
    }
} 