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