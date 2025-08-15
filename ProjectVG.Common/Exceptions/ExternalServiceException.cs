namespace ProjectVG.Common.Exceptions
{
    public class ExternalServiceException : ProjectVGException
    {
        public string ServiceName { get; }
        public string Endpoint { get; }

        public ExternalServiceException(string serviceName, string endpoint, string message, ErrorCode errorCode = ErrorCode.EXTERNAL_SERVICE_ERROR) 
            : base(errorCode, message, 502)
        {
            ServiceName = serviceName;
            Endpoint = endpoint;
        }

        public ExternalServiceException(string serviceName, string endpoint, Exception innerException, ErrorCode errorCode = ErrorCode.EXTERNAL_SERVICE_ERROR) 
            : base(errorCode, $"외부 서비스 '{serviceName}' ({endpoint})에서 오류가 발생했습니다", innerException, 502)
        {
            ServiceName = serviceName;
            Endpoint = endpoint;
        }
    }
} 