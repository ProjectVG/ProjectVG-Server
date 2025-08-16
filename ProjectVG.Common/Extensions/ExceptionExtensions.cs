using ProjectVG.Common.Exceptions;

namespace ProjectVG.Common.Extensions
{
    public static class ExceptionExtensions
    {
        /// <summary>
        /// 예외 객체를 HTTP 상태 코드로 변환합니다.
        /// </summary>
        /// <param name="exception">상태 코드로 변환할 예외 인스턴스.</param>
        /// <returns>
        /// 매핑된 HTTP 상태 코드. ProjectVGException이면 해당 인스턴스의 <c>StatusCode</c>를 반환합니다.
        /// 그 외의 타입은 다음과 같이 매핑됩니다: <c>NotFoundException</c> → 404, <c>ValidationException</c> → 400, <c>ExternalServiceException</c> → 502, 그 외 모든 예외 → 500.
        /// </returns>
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

        /// <summary>
        /// 예외 인스턴트에 대응하는 표준화된 오류 코드 문자열을 반환합니다.
        /// </summary>
        /// <param name="exception">오류 코드를 결정할 대상 예외.</param>
        /// <returns>
        /// ProjectVGException이면 해당 인스턴스의 ErrorCode.GetCode()를 반환하고,
        /// 그렇지 않으면 예외 타입에 따라 사전 정의된 ErrorCode의 GetCode() 값을 반환합니다:
        /// NotFoundException -> NOT_FOUND, ValidationException -> VALIDATION_FAILED,
        /// ExternalServiceException -> EXTERNAL_SERVICE_ERROR, 그 외 -> INTERNAL_SERVER_ERROR.
        /// </returns>
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