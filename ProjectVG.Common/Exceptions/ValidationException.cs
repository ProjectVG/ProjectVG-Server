using System.ComponentModel.DataAnnotations;

namespace ProjectVG.Common.Exceptions
{
    public class ValidationException : ProjectVGException
    {
        public List<ValidationResult> ValidationErrors { get; }

        /// <summary>
        /// 지정된 오류 코드를 사용해 새 ValidationException 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="errorCode">예외의 원인이 되는 오류 코드; 예외 메시지는 해당 코드의 GetMessage() 결과로 설정됩니다.</param>
        public ValidationException(ErrorCode errorCode)
    : base(errorCode, errorCode.GetMessage(), 400)
        {
            ValidationErrors = new List<ValidationResult>();
        }

        /// <summary>
        /// 지정된 오류 코드와 검증 결과 목록으로 ValidationException 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="errorCode">예외에 대응되는 ErrorCode. 이 코드의 메시지(errorCode.GetMessage())가 예외 메시지로 사용됩니다.</param>
        /// <param name="validationErrors">발생한 검증 결과(ValidationResult)의 목록으로, 예외의 ValidationErrors 프로퍼티에 그대로 할당됩니다.</param>
        /// <remarks>
        /// 생성자는 상위(ProjectVGException) 생성자에 HTTP 상태 코드 400과 errorCode의 메시지를 전달합니다.
        /// validationErrors는 null일 수 있으며(호출자가 전달한 값을 그대로 사용), 예외의 ValidationErrors에 설정됩니다.
        /// </remarks>
        public ValidationException(ErrorCode errorCode, List<ValidationResult> validationErrors)
    : base(errorCode, errorCode.GetMessage(), 400)
        {
            ValidationErrors = validationErrors;
        }

        /// <summary>
        /// 지정한 오류 코드와 메시지, 검증 결과 목록으로 새로운 ValidationException 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="errorCode">예외에 대응되는 ErrorCode.</param>
        /// <param name="message">예외 메시지로 사용될 문자열.</param>
        /// <param name="validationErrors">검증 실패 정보를 담은 ValidationResult 목록. ValidationErrors 프로퍼티에 할당됩니다.</param>
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