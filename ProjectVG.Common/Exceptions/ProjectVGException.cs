namespace ProjectVG.Common.Exceptions
{
    public class ProjectVGException : Exception
    {
        public string ErrorCode { get; }
        public int StatusCode { get; }

        public ProjectVGException(string message, string errorCode, int statusCode = 500) 
            : base(message)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }

        public ProjectVGException(string message, string errorCode, Exception innerException, int statusCode = 500) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
} 