using System.ComponentModel.DataAnnotations;

namespace ProjectVG.Common.Exceptions
{
    public class ValidationException : ProjectVGException
    {
        public List<ValidationResult> ValidationErrors { get; }

        public ValidationException(string message, List<ValidationResult> validationErrors, string errorCode = "일반_유효성_검사_실패") 
            : base(message, errorCode, 400)
        {
            ValidationErrors = validationErrors;
        }

        public ValidationException(string message, string errorCode = "일반_유효성_검사_실패") 
            : base(message, errorCode, 400)
        {
            ValidationErrors = new List<ValidationResult>();
        }
    }
} 