using ProjectVG.Common.Exceptions;

namespace ProjectVG.Common.Extensions
{
    public static class ExceptionExtensions
    {
        public static int GetStatusCode(this Exception exception)
        {
            return exception switch
            {
                ProjectVGException pgException => pgException.StatusCode,
                NotFoundException => 404,
                ValidationException => 400,
                UnauthorizedException => 401,
                ExternalServiceException => 502,
                _ => 500
            };
        }

        public static string GetErrorCode(this Exception exception)
        {
            return exception switch
            {
                ProjectVGException pgException => pgException.ErrorCode,
                NotFoundException => ErrorCodes.일반_찾을_수_없음,
                ValidationException => ErrorCodes.일반_유효성_검사_실패,
                UnauthorizedException => ErrorCodes.인증_실패,
                ExternalServiceException => ErrorCodes.외부_서비스_오류,
                _ => ErrorCodes.일반_내부_오류
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