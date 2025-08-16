namespace ProjectVG.Application.Models.Chat
{
    public class ChatValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;

        public static ChatValidationResult Success()
        {
            return new ChatValidationResult { IsValid = true };
        }

        /// <summary>
        /// 유효성 검사 실패를 나타내는 ChatValidationResult 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="message">오류 설명으로 사용될 메시지.</param>
        /// <param name="errorCode">오류 코드(기본값: "VALIDATION_ERROR").</param>
        /// <returns>IsValid가 false이며 ErrorMessage와 ErrorCode가 설정된 ChatValidationResult.</returns>
        public static ChatValidationResult Failure(string message, string errorCode = "VALIDATION_ERROR")
        {
            return new ChatValidationResult 
            { 
                IsValid = false, 
                ErrorMessage = message, 
                ErrorCode = errorCode 
            };
        }
    }
}
