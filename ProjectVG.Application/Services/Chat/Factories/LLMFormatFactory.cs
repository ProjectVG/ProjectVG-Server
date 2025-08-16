using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat.Factories
{
    public static class LLMFormatFactory
    {
        public static ChatLLMFormat CreateChatFormat()
        {
            return new ChatLLMFormat();
        }

        public static UserInputAnalysisLLMFormat CreateUserInputAnalysisFormat()
        {
            return new UserInputAnalysisLLMFormat();
        }
    }
}
