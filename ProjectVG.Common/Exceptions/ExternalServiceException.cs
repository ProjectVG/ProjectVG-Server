namespace ProjectVG.Common.Exceptions
{
    public class ExternalServiceException : ProjectVGException
    {
        public string ServiceName { get; }
        public string Endpoint { get; }

        /// <summary>
        /// 외부 서비스 호출 중 발생한 오류를 나타내는 예외를 초기화합니다.
        /// </summary>
        /// <param name="serviceName">오류가 발생한 외부 서비스의 이름.</param>
        /// <param name="endpoint">오류가 발생한 외부 서비스의 엔드포인트(URI 또는 식별자).</param>
        /// <param name="message">사용자에게 표시되거나 로그에 기록할 예외 메시지.</param>
        /// <param name="errorCode">연관된 애플리케이션 오류 코드(기본값: <c>ErrorCode.EXTERNAL_SERVICE_ERROR</c>).</param>
        /// <remarks>
        /// 생성된 예외는 내부적으로 HTTP 상태 코드 502(Bad Gateway)를 사용하며, ServiceName 및 Endpoint 속성을 설정합니다.
        /// </remarks>
        public ExternalServiceException(string serviceName, string endpoint, string message, ErrorCode errorCode = ErrorCode.EXTERNAL_SERVICE_ERROR) 
            : base(errorCode, message, 502)
        {
            ServiceName = serviceName;
            Endpoint = endpoint;
        }

        /// <summary>
        /// 외부 서비스에서 발생한 오류를 나타내는 예외를 초기화합니다.
        /// </summary>
        /// <param name="serviceName">오류가 발생한 외부 서비스의 이름.</param>
        /// <param name="endpoint">오류가 발생한 외부 서비스의 엔드포인트(주소).</param>
        /// <param name="innerException">원인 예외(감싸진 예외).</param>
        /// <param name="errorCode">연결된 오류 코드(기본값: <see cref="ErrorCode.EXTERNAL_SERVICE_ERROR"/>).</param>
        /// <remarks>
        /// 생성된 예외는 내부 예외를 포함하고 서비스 이름과 엔드포인트 정보를 보관합니다.
        /// 또한 기본적으로 HTTP 상태 코드 502(Bad Gateway)를 나타내도록 설정됩니다.
        /// </remarks>
        public ExternalServiceException(string serviceName, string endpoint, Exception innerException, ErrorCode errorCode = ErrorCode.EXTERNAL_SERVICE_ERROR) 
            : base(errorCode, $"외부 서비스 '{serviceName}' ({endpoint})에서 오류가 발생했습니다", innerException, 502)
        {
            ServiceName = serviceName;
            Endpoint = endpoint;
        }
    }
} 