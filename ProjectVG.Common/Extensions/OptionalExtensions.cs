using ProjectVG.Common.Exceptions;

namespace ProjectVG.Common.Extensions
{
    public static class OptionalExtensions
    {
        /// <summary>
        /// 널이 될 수 있는 참조형 값이 null이 아니면 그 값을 반환하고, null이면 제공된 예외를 던집니다.
        /// </summary>
        /// <param name="value">확인할 nullable 참조형 값.</param>
        /// <param name="exceptionSupplier">값이 null일 때 던질 예외를 생성하는 함수.</param>
        /// <returns>null이 아님이 보장된 값 <typeparamref name="T"/>.</returns>
        /// <exception cref="System.Exception">value가 null일 경우 <paramref name="exceptionSupplier"/>가 반환한 예외를 던집니다.</exception>
        public static T OrElseThrow<T>(this T? value, Func<Exception> exceptionSupplier) where T : class
        {
            return value ?? throw exceptionSupplier();
        }

        /// <summary>
        /// 널일 수 있는 참조 값을 반환하거나, 값이 없으면 지정한 메시지와 오류 코드로 NotFoundException을 던집니다.
        /// </summary>
        /// <param name="value">검사할 nullable 참조값. 값이 존재하면 그대로 반환됩니다.</param>
        /// <param name="message">값이 없을 때 NotFoundException에 사용될 설명 메시지입니다.</param>
        /// <param name="errorCode">값이 없을 때 NotFoundException에 사용될 오류 코드(기본값: ErrorCode.NOT_FOUND).</param>
        /// <returns>널이 아닌 원래의 값(T).</returns>
        /// <exception cref="ProjectVG.Common.Exceptions.NotFoundException">value가 null인 경우 지정한 message와 errorCode로 발생합니다.</exception>
        public static T OrElseThrow<T>(this T? value, string message, ErrorCode errorCode = ErrorCode.NOT_FOUND) where T : class
        {
            return value ?? throw new NotFoundException(errorCode, message);
        }

        /// <summary>
        /// nullable 참조형식 값을 반환하거나, 값이 null이면 지정한 자원 이름과 ID를 포함한 메시지로 NotFoundException을 던집니다.
        /// </summary>
        /// <param name="value">반환할 수 있는 대상 객체 (확인 후 반환됨).</param>
        /// <param name="resourceName">찾으려는 자원의 이름(예: 엔티티 이름).</param>
        /// <param name="id">자원의 식별자 값으로, 메시지에 포함됩니다.</param>
        /// <param name="errorCode">예외에 설정할 ErrorCode(기본값: ErrorCode.NOT_FOUND).</param>
        /// <returns>value가 null이 아니면 해당 값을 반환합니다.</returns>
        /// <exception cref="ProjectVG.Common.Exceptions.NotFoundException">value가 null일 때, 지정한 errorCode와 "{resourceName} (ID: {id})를 찾을 수 없습니다" 메시지로 던져집니다.</exception>
        public static T OrElseThrow<T>(this T? value, string resourceName, object id, ErrorCode errorCode = ErrorCode.NOT_FOUND) where T : class
        {
            return value ?? throw new NotFoundException(errorCode, $"{resourceName} (ID: {id})를 찾을 수 없습니다");
        }
    }
}
