using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat
{
    public interface IChatService
    {
        Task<ChatRequestResult> EnqueueChatRequestAsync(ChatRequestCommand command);
    }
} 