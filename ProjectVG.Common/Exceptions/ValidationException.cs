using System.ComponentModel.DataAnnotations;

namespace ProjectVG.Common.Exceptions
{
    public class ValidationException : ProjectVGException
    {
        public List<ValidationResult> ValidationErrors { get; }

        /// <summary>
        /// 지정된 오류 코드와 메시지 및 검증 결과 집합으로 새 유효성 검사 예외를 생성합니다.
        /// </summary>
        /// <param name="validationErrors">예외에 포함할 개별 검증 실패 항목들의 목록 (System.ComponentModel.DataAnnotations.ValidationResult).</param>
        /// <remarks>이 예외는 항상 HTTP 상태 코드 400(Bad Request)로 처리됩니다.</remarks>
        public ValidationException(ErrorCode errorCode, string message, List<ValidationResult> validationErrors) 
            : base(errorCode, message, 400)
        {
            ValidationErrors = validationErrors;
        }

        /// <summary>
        /// 지정된 오류 코드와 메시지로 유효성 검사 예외를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 이 생성자는 예외의 HTTP 상태 코드를 400(Bad Request)으로 설정하며, 개별 검증 결과를 담는 <see cref="ValidationErrors"/>는 빈 목록으로 초기화됩니다.
        /// </remarks>
        /// <param name="errorCode">예외에 연관된 애플리케이션 수준의 오류 코드.</param>
        /// <param name="message">예외에 대한 설명 메시지.</param>
        public ValidationException(ErrorCode errorCode, string message) 
            : base(errorCode, message, 400)
        {
            ValidationErrors = new List<ValidationResult>();
        }

        /// <summary>
        /// 지정된 ErrorCode와 값으로 예외를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 생성된 예외는 내부 메시지를 "{errorCode.GetMessage()}: {value}" 형식으로 구성하고 HTTP 상태 코드를 400(Bad Request)로 설정합니다.
        /// 또한 ValidationErrors 프로퍼티는 빈 리스트로 초기화됩니다.
        /// </remarks>
        /// <param name="errorCode">예외에 대응하는 ErrorCode.</param>
        /// <param name="value">메시지에 포함할 추가 값(예: 유효성 검사 실패 대상).</param>
        public ValidationException(ErrorCode errorCode, object value) 
            : base(errorCode, $"{errorCode.GetMessage()}: {value}", 400)
        {
            ValidationErrors = new List<ValidationResult>();
        }
    }
} 