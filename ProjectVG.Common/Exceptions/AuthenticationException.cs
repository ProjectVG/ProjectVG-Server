namespace ProjectVG.Common.Exceptions
{
    /// <summary>
    /// 인증 실패 시 사용되는 예외로 기본 상태 코드는 401이다.
    /// </summary>
    public class AuthenticationException : ProjectVGException
    {
        /// <summary>
        /// 인증 실패를 나타내는 AuthenticationException 인스턴스를 생성합니다. 이 생성자는 기본 HTTP 상태 코드 401을 사용합니다.
        /// </summary>
        /// <param name="errorCode">해당 예외를 식별하는 오류 코드.</param>
        public AuthenticationException(Constants.ErrorCode errorCode)
            : base(errorCode, 401)
        {
        }

        /// <summary>
        /// 지정된 에러 코드와 사용자 지정 메시지로 인증 실패를 나타내는 예외를 생성합니다. 생성된 예외의 HTTP 상태 코드는 401(권한 없음)으로 설정됩니다.
        /// </summary>
        /// <param name="errorCode">예외에 대응되는 내부 에러 코드.</param>
        /// <param name="customMessage">사용자에게 전달할 상세 메시지.</param>
        public AuthenticationException(Constants.ErrorCode errorCode, string customMessage)
            : base(errorCode, customMessage, 401)
        {
        }

        /// <summary>
        /// 인증 실패를 나타내는 예외를 생성합니다. 내부 예외를 포함하며 HTTP 상태 코드는 401(Unauthorized)으로 고정됩니다.
        /// </summary>
        /// <param name="errorCode">발생한 오류를 나타내는 상수형 오류 코드.</param>
        /// <param name="innerException">원인이 되는 내부 예외 (있을 경우).</param>
        public AuthenticationException(Constants.ErrorCode errorCode, Exception innerException)
            : base(errorCode, innerException, 401)
        {
        }

        /// <summary>
        /// 인증 실패를 나타내는 AuthenticationException 인스턴스를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 지정한 오류 코드, 사용자 정의 메시지 및 내부 예외로 예외를 초기화하고 HTTP 상태 코드를 401(Unauthorized)로 설정합니다.
        /// </remarks>
        /// <param name="errorCode">프로젝트 고유의 오류 코드.</param>
        /// <param name="customMessage">클라이언트에 표시하거나 로깅에 사용할 사용자 정의 메시지.</param>
        /// <param name="innerException">원인이 되는 내부 예외(있을 경우).</param>
        public AuthenticationException(Constants.ErrorCode errorCode, string customMessage, Exception innerException)
            : base(errorCode, customMessage, innerException, 401)
        {
        }
    }
}
