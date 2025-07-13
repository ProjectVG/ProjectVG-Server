namespace ProjectVG.Common.Exceptions
{
    public class ExternalServiceException : ProjectVGException
    {
        public string ServiceName { get; }
        public string Endpoint { get; }

        public ExternalServiceException(string serviceName, string endpoint, string message, string errorCode = "외부_서비스_오류") 
            : base(message, errorCode, 502)
        {
            ServiceName = serviceName;
            Endpoint = endpoint;
        }

        public ExternalServiceException(string serviceName, string endpoint, Exception innerException, string errorCode = "외부_서비스_오류") 
            : base($"외부 서비스 '{serviceName}' ({endpoint})에서 오류가 발생했습니다", errorCode, innerException, 502)
        {
            ServiceName = serviceName;
            Endpoint = endpoint;
        }
    }
} 