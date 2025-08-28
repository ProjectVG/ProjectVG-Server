namespace ProjectVG.Common.Exceptions
{
    /// <summary>
    /// 인증 실패 시 사용되는 예외로 기본 상태 코드는 401이다.
    /// </summary>
    public class AuthenticationException : ProjectVGException
    {
        public AuthenticationException(Constants.ErrorCode errorCode)
            : base(errorCode, 401)
        {
        }

        public AuthenticationException(Constants.ErrorCode errorCode, string customMessage)
            : base(errorCode, customMessage, 401)
        {
        }

        public AuthenticationException(Constants.ErrorCode errorCode, Exception innerException)
            : base(errorCode, innerException, 401)
        {
        }

        public AuthenticationException(Constants.ErrorCode errorCode, string customMessage, Exception innerException)
            : base(errorCode, customMessage, innerException, 401)
        {
        }
    }
}
