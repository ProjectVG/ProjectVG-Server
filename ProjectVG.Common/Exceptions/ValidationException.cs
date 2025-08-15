using System.ComponentModel.DataAnnotations;

namespace ProjectVG.Common.Exceptions
{
    public class ValidationException : ProjectVGException
    {
        public List<ValidationResult> ValidationErrors { get; }

        public ValidationException(ErrorCode errorCode, string message, List<ValidationResult> validationErrors) 
            : base(errorCode, message, 400)
        {
            ValidationErrors = validationErrors;
        }

        public ValidationException(ErrorCode errorCode, string message) 
            : base(errorCode, message, 400)
        {
            ValidationErrors = new List<ValidationResult>();
        }

        public ValidationException(ErrorCode errorCode, object value) 
            : base(errorCode, $"{errorCode.GetMessage()}: {value}", 400)
        {
            ValidationErrors = new List<ValidationResult>();
        }
    }
} 