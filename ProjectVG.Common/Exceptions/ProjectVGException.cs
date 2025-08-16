namespace ProjectVG.Common.Exceptions
{
    public class ProjectVGException : Exception
    {
        public ErrorCode ErrorCode { get; }
        public int StatusCode { get; }

        /// <summary>
        /// 지정된 ErrorCode로부터 메시지를 가져와 예외를 생성하고 ErrorCode와 HTTP 상태 코드를 설정합니다.
        /// </summary>
        /// <param name="errorCode">도메인 오류를 나타내는 값. 예외 메시지는 errorCode.GetMessage()로 설정됩니다.</param>
        /// <param name="statusCode">연관된 HTTP 상태 코드(기본값: 500).</param>
        public ProjectVGException(ErrorCode errorCode, int statusCode = 500) 
            : base(errorCode.GetMessage())
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }

        /// <summary>
        /// 지정된 ErrorCode의 메시지와 내부 예외를 사용하여 ProjectVG 예외를 생성합니다.
        /// </summary>
        /// <param name="errorCode">도메인 오류를 식별하는 ErrorCode 열거값(예외의 ErrorCode 속성에 할당됨).</param>
        /// <param name="innerException">원인이 되는 내부 예외(원인 예외를 래핑함).</param>
        /// <param name="statusCode">연결된 HTTP 상태 코드(기본값: 500). 이 값은 StatusCode 속성에 설정됩니다.</param>
        public ProjectVGException(ErrorCode errorCode, Exception innerException, int statusCode = 500) 
            : base(errorCode.GetMessage(), innerException)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }

        /// <summary>
        /// 지정한 사용자 메시지와 오류 코드를 사용해 ProjectVG 전용 예외를 생성합니다.
        /// </summary>
        /// <param name="errorCode">예외에 연결할 도메인별 ErrorCode.</param>
        /// <param name="customMessage">예외의 기본 메시지로 사용될 사용자 지정 문자열.</param>
        /// <param name="statusCode">연결할 HTTP 상태 코드(기본값: 500).</param>
        public ProjectVGException(ErrorCode errorCode, string customMessage, int statusCode = 500) 
            : base(customMessage)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }

        /// <summary>
        /// 지정된 오류 코드와 사용자 정의 메시지, 내부 예외, HTTP 상태 코드를 사용해 ProjectVG 예외를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 기본 예외 메시지는 전달된 사용자 정의 메시지(customMessage)를 사용합니다.
        /// </remarks>
        /// <param name="errorCode">도메인 오류를 나타내는 ErrorCode 값.</param>
        /// <param name="customMessage">예외에 사용할 사용자 정의 메시지(에러 코드의 기본 메시지를 대체).</param>
        /// <param name="innerException">이 예외의 원인이 되는 내부 예외(없으면 null).</param>
        /// <param name="statusCode">연관된 HTTP 상태 코드(기본값 500).</param>
        public ProjectVGException(ErrorCode errorCode, string customMessage, Exception innerException, int statusCode = 500) 
            : base(customMessage, innerException)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
} 