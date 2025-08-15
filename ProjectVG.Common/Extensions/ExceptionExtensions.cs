using ProjectVG.Common.Exceptions;

namespace ProjectVG.Common.Extensions
{
    public static class ExceptionExtensions
    {
        public static int GetStatusCode(this Exception exception)
        {
            if (exception is ProjectVGException pgException)
                return pgException.StatusCode;
            
            return exception switch
            {
                NotFoundException => 404,
                ValidationException => 400,
                ExternalServiceException => 502,
                _ => 500
            };
        }

        public static string GetErrorCode(this Exception exception)
        {
            if (exception is ProjectVGException pgException)
                return pgException.ErrorCode.GetCode();
            
            return exception switch
            {
                NotFoundException => ErrorCode.NOT_FOUND.GetCode(),
                ValidationException => ErrorCode.VALIDATION_FAILED.GetCode(),
                ExternalServiceException => ErrorCode.EXTERNAL_SERVICE_ERROR.GetCode(),
                _ => ErrorCode.INTERNAL_SERVER_ERROR.GetCode()
            };
        }

        public static bool IsClientError(this Exception exception)
        {
            var statusCode = exception.GetStatusCode();
            return statusCode >= 400 && statusCode < 500;
        }

        public static bool IsServerError(this Exception exception)
        {
            var statusCode = exception.GetStatusCode();
            return statusCode >= 500;
        }
    }
} 