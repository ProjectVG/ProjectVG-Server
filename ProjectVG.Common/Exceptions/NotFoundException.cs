namespace ProjectVG.Common.Exceptions
{
    public class NotFoundException : ProjectVGException
    {
        /// <summary>
        /// 요청한 리소스를 찾을 수 없음을 나타내는 NotFoundException을 생성합니다.
        /// </summary>
        /// <param name="errorCode">예외에 할당할 에러 코드. 기본값은 <see cref="ErrorCode.NOT_FOUND"/>입니다. 이 생성자는 항상 HTTP 상태 코드 404를 사용합니다.</param>
        public NotFoundException(ErrorCode errorCode = ErrorCode.NOT_FOUND) 
            : base(errorCode, 404)
        {
        }

        /// <summary>
        /// 지정한 ErrorCode와 사용자 정의 메시지로 Not Found(404) 예외를 생성합니다.
        /// </summary>
        /// <param name="errorCode">예외를 식별하는 ErrorCode. 기본적으로 NOT_FOUND을 사용합니다.</param>
        /// <param name="customMessage">예외에 사용될 사용자 정의 메시지.</param>
        /// <remarks>이 생성자는 내부적으로 HTTP 상태 코드 404를 사용합니다.</remarks>
        public NotFoundException(ErrorCode errorCode, string customMessage) 
            : base(errorCode, customMessage, 404)
        {
        }

        /// <summary>
        /// 지정된 오류 코드와 식별자(ID)를 조합해 "오류메시지: {id}" 형태의 메시지로 초기화된 NotFoundException을 생성합니다.
        /// 상태 코드는 항상 404로 설정됩니다.
        /// </summary>
        /// <param name="errorCode">예외를 나타내는 ErrorCode. GetMessage()의 반환값이 메시지 앞부분으로 사용됩니다.</param>
        /// <param name="id">찾을 리소스의 식별자; 메시지의 끝부분에 포함됩니다.</param>
        public NotFoundException(ErrorCode errorCode, object id) 
            : base(errorCode, $"{errorCode.GetMessage()}: {id}", 404)
        {
        }

        /// <summary>
        /// 지정한 리소스 이름과 식별자(ID)를 사용해 "찾을 수 없음" 예외를 생성합니다. 내부적으로 404 상태 코드와
        /// "{resourceName} (ID: {id})를 찾을 수 없습니다" 형식의 메시지를 설정합니다.
        /// </summary>
        /// <param name="errorCode">예외를 식별하는 ErrorCode 값(기본: <c>ErrorCode.NOT_FOUND</c>).</param>
        /// <param name="resourceName">찾을 리소스의 이름(메시지에 그대로 사용됨).</param>
        /// <param name="id">찾을 리소스의 식별자(ID; 메시지에 포함됨).</param>
        public NotFoundException(ErrorCode errorCode, string resourceName, object id) 
            : base(errorCode, $"{resourceName} (ID: {id})를 찾을 수 없습니다", 404)
        {
        }
    }
} 