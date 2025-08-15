namespace ProjectVG.Common.Exceptions
{
    public class ProjectVGException : Exception
    {
        public ErrorCode ErrorCode { get; }
        public int StatusCode { get; }

        public ProjectVGException(ErrorCode errorCode, int statusCode = 500) 
            : base(errorCode.GetMessage())
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }

        public ProjectVGException(ErrorCode errorCode, Exception innerException, int statusCode = 500) 
            : base(errorCode.GetMessage(), innerException)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }

        public ProjectVGException(ErrorCode errorCode, string customMessage, int statusCode = 500) 
            : base(customMessage)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }

        public ProjectVGException(ErrorCode errorCode, string customMessage, Exception innerException, int statusCode = 500) 
            : base(customMessage, innerException)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
} 