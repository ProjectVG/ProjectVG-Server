namespace ProjectVG.Application.Models.Chat
{
    public class ChatPreprocessResult
    {
        public bool IsValid { get; private set; }
        public ChatRequestResponse? RequestResponse { get; private set; }
        public ChatPreprocessContext? Context { get; private set; }

        /// <summary>
        /// 전처리가 성공한 ChatPreprocessResult 인스턴스를 생성하여 반환합니다.
        /// </summary>
        /// <param name="context">성공한 전처리의 컨텍스트를 포함하는 ChatPreprocessContext.</param>
        /// <returns>IsValid가 true이며 지정된 컨텍스트를 가진 ChatPreprocessResult.</returns>
        public static ChatPreprocessResult Success(ChatPreprocessContext context)
        {
            return new ChatPreprocessResult
            {
                IsValid = true,
                Context = context
            };
        }

        /// <summary>
        /// 실패한 전처리 결과를 나타내는 ChatPreprocessResult 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="requestResponse">전처리 실패를 설명하는 요청/응답 정보(예: 오류 코드 및 메시지).</param>
        /// <returns>IsValid가 false이고 RequestResponse가 지정된 ChatPreprocessResult 객체.</returns>
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
