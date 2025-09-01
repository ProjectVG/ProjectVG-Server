namespace ProjectVG.Application.Models.Chat
{
    public record ChatValidationResult
    {
        public bool IsValid { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
        public string ErrorCode { get; init; } = string.Empty;

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
