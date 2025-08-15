using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Models.Chat
{
    public class ChatPreprocessResult
    {
        public bool IsValid { get; private set; }
        public ChatRequestResponse? RequestResponse { get; private set; }
        public ChatPreprocessContext? Context { get; private set; }

        public static ChatPreprocessResult Success(ChatPreprocessContext context)
        {
            return new ChatPreprocessResult
            {
                IsValid = true,
                Context = context
            };
        }

        public static ChatPreprocessResult Failure(ChatRequestResponse requestResponse)
        {
            return new ChatPreprocessResult
            {
                IsValid = false,
                RequestResponse = requestResponse
            };
        }
    }
}
