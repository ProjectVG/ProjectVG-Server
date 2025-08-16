namespace ProjectVG.Common.Constants
{
    public enum ErrorCode
    {
        // 일반 오류
        INTERNAL_SERVER_ERROR,
        BAD_REQUEST,
        UNAUTHORIZED,
        FORBIDDEN,
        NOT_FOUND,
        CONFLICT,
        VALIDATION_FAILED,
        
        // 사용자 관련 오류
        USER_NOT_FOUND,
        USER_ALREADY_EXISTS,
        EMAIL_ALREADY_EXISTS,
        USERNAME_ALREADY_EXISTS,
        INVALID_CREDENTIALS,
        
        // 캐릭터 관련 오류
        CHARACTER_NOT_FOUND,
        CHARACTER_ALREADY_EXISTS,
        
        // 대화 관련 오류
        CONVERSATION_NOT_FOUND,
        MESSAGE_TOO_LONG,
        MESSAGE_EMPTY,
        INVALID_INPUT,
        INAPPROPRIATE_REQUEST,
        UNKNOWN_ACTION,
        
        // 외부 서비스 오류
        EXTERNAL_SERVICE_ERROR,
        LLM_SERVICE_ERROR,
        TTS_SERVICE_ERROR,
        MEMORY_SERVICE_ERROR,
        
        // 데이터베이스 오류
        DATABASE_ERROR,
        DATABASE_CONNECTION_ERROR,
        DATABASE_CONSTRAINT_VIOLATION,
        
        // 인증/권한 오류
        AUTHENTICATION_FAILED,
        AUTHORIZATION_FAILED,
        TOKEN_EXPIRED,
        TOKEN_INVALID,
        INVALID_SESSION_ID
    }

    public static class ErrorCodeExtensions
    {
        private static readonly Dictionary<ErrorCode, string> _errorMessages = new()
        {
            // 일반 오류
            { ErrorCode.INTERNAL_SERVER_ERROR, "서버에서 예상치 못한 오류가 발생했습니다" },
            { ErrorCode.BAD_REQUEST, "잘못된 요청입니다" },
            { ErrorCode.UNAUTHORIZED, "인증이 필요합니다" },
            { ErrorCode.FORBIDDEN, "접근 권한이 없습니다" },
            { ErrorCode.NOT_FOUND, "요청한 리소스를 찾을 수 없습니다" },
            { ErrorCode.CONFLICT, "리소스 충돌이 발생했습니다" },
            { ErrorCode.VALIDATION_FAILED, "입력 데이터가 유효하지 않습니다" },
            
            // 사용자 관련 오류
            { ErrorCode.USER_NOT_FOUND, "사용자를 찾을 수 없습니다" },
            { ErrorCode.USER_ALREADY_EXISTS, "이미 존재하는 사용자입니다" },
            { ErrorCode.EMAIL_ALREADY_EXISTS, "이미 사용 중인 이메일입니다" },
            { ErrorCode.USERNAME_ALREADY_EXISTS, "이미 사용 중인 사용자명입니다" },
            { ErrorCode.INVALID_CREDENTIALS, "잘못된 인증 정보입니다" },
            
            // 캐릭터 관련 오류
            { ErrorCode.CHARACTER_NOT_FOUND, "캐릭터를 찾을 수 없습니다" },
            { ErrorCode.CHARACTER_ALREADY_EXISTS, "이미 존재하는 캐릭터입니다" },
            
            // 대화 관련 오류
            { ErrorCode.CONVERSATION_NOT_FOUND, "대화를 찾을 수 없습니다" },
            { ErrorCode.MESSAGE_TOO_LONG, "메시지가 너무 깁니다" },
            { ErrorCode.MESSAGE_EMPTY, "메시지 내용이 비어있습니다" },
            { ErrorCode.INVALID_INPUT, "잘못된 입력입니다" },
            { ErrorCode.INAPPROPRIATE_REQUEST, "부적절한 요청입니다" },
            { ErrorCode.UNKNOWN_ACTION, "알 수 없는 액션입니다" },
            
            // 외부 서비스 오류
            { ErrorCode.EXTERNAL_SERVICE_ERROR, "외부 서비스에서 오류가 발생했습니다" },
            { ErrorCode.LLM_SERVICE_ERROR, "AI 서비스에서 오류가 발생했습니다" },
            { ErrorCode.TTS_SERVICE_ERROR, "음성 변환 서비스에서 오류가 발생했습니다" },
            { ErrorCode.MEMORY_SERVICE_ERROR, "메모리 서비스에서 오류가 발생했습니다" },
            
            // 데이터베이스 오류
            { ErrorCode.DATABASE_ERROR, "데이터베이스 처리 중 오류가 발생했습니다" },
            { ErrorCode.DATABASE_CONNECTION_ERROR, "데이터베이스 연결에 실패했습니다" },
            { ErrorCode.DATABASE_CONSTRAINT_VIOLATION, "데이터베이스 제약 조건을 위반했습니다" },
            
            // 인증/권한 오류
            { ErrorCode.AUTHENTICATION_FAILED, "인증에 실패했습니다" },
            { ErrorCode.AUTHORIZATION_FAILED, "권한이 부족합니다" },
            { ErrorCode.TOKEN_EXPIRED, "토큰이 만료되었습니다" },
            { ErrorCode.TOKEN_INVALID, "유효하지 않은 토큰입니다" },
            { ErrorCode.INVALID_SESSION_ID, "유효하지 않은 세션 ID입니다" }
        };

        /// <summary>
        /// 지정된 ErrorCode에 대응하는 한국어 설명 문자열을 반환합니다.
        /// 실패할 경우 INTERNAL_SERVER_ERROR에 대한 설명으로 대체 반환합니다.
        /// </summary>
        /// <param name="errorCode">메시지를 조회할 ErrorCode 값.</param>
        /// <returns>해당 ErrorCode의 한국어 메시지 문자열. 매핑이 없으면 INTERNAL_SERVER_ERROR 메시지.</returns>
        public static string GetMessage(this ErrorCode errorCode)
        {
            return _errorMessages.TryGetValue(errorCode, out var message) ? message : ErrorCode.INTERNAL_SERVER_ERROR.GetMessage();
        }

        /// <summary>
        /// 지정된 ErrorCode 열거값의 문자열 표현(코드 이름)을 반환합니다.
        /// </summary>
        /// <returns>해당 ErrorCode의 이름(예: "INTERNAL_SERVER_ERROR")을 나타내는 문자열.</returns>
        public static string GetCode(this ErrorCode errorCode)
        {
            return errorCode.ToString();
        }
    }
} 