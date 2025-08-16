using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat.Factories
{
    public static class LLMFormatFactory
    {
        /// <summary>
        /// 새 ChatLLMFormat 인스턴스를 생성하여 반환합니다.
        /// </summary>
        /// <returns>새로 생성된 <see cref="ChatLLMFormat"/> 인스턴스.</returns>
        public static ChatLLMFormat CreateChatFormat()
        {
            return new ChatLLMFormat();
        }

        /// <summary>
        /// 사용자 입력 분석용 LLM 형식(UserInputAnalysisLLMFormat)의 새 인스턴스를 생성하여 반환합니다.
        /// </summary>
        /// <returns>기본 생성자로 초기화된 <see cref="UserInputAnalysisLLMFormat"/> 인스턴스.</returns>
        public static UserInputAnalysisLLMFormat CreateUserInputAnalysisFormat()
        {
            return new UserInputAnalysisLLMFormat();
        }
    }
}
