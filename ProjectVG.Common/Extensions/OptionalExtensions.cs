using ProjectVG.Common.Exceptions;

namespace ProjectVG.Common.Extensions
{
    public static class OptionalExtensions
    {
        public static T OrElseThrow<T>(this T? value, Func<Exception> exceptionSupplier) where T : class
        {
            return value ?? throw exceptionSupplier();
        }

        public static T OrElseThrow<T>(this T? value, string message, ErrorCode errorCode = ErrorCode.NOT_FOUND) where T : class
        {
            return value ?? throw new NotFoundException(errorCode, message);
        }

        public static T OrElseThrow<T>(this T? value, string resourceName, object id, ErrorCode errorCode = ErrorCode.NOT_FOUND) where T : class
        {
            return value ?? throw new NotFoundException(errorCode, $"{resourceName} (ID: {id})를 찾을 수 없습니다");
        }
    }
}
