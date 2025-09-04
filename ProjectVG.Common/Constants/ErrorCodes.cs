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
        USER_CREATION_FAILED,
        USER_UPDATE_FAILED,
        
        // 캐릭터 관련 오류
        CHARACTER_NOT_FOUND,
        CHARACTER_ALREADY_EXISTS,
        CHARACTER_CREATION_FAILED,
        CHARACTER_UPDATE_FAILED,
        
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
        TOKEN_MISSING,
        TOKEN_REFRESH_FAILED,
        TOKEN_REVOKE_FAILED,
        INVALID_SESSION_ID,
        
        // OAuth2 관련 오류
        OAUTH2_PROVIDER_NOT_SUPPORTED,
        OAUTH2_PROVIDER_NOT_CONFIGURED,
        OAUTH2_CLIENT_ID_INVALID,
        OAUTH2_CLIENT_SECRET_INVALID,
        OAUTH2_REDIRECT_URI_INVALID,
        OAUTH2_AUTHORIZATION_CODE_INVALID,
        OAUTH2_STATE_INVALID,
        OAUTH2_PKCE_INVALID,
        OAUTH2_TOKEN_EXCHANGE_FAILED,
        OAUTH2_USER_INFO_FAILED,
        OAUTH2_CALLBACK_FAILED,
        OAUTH2_REQUEST_EXPIRED,
        OAUTH2_REQUEST_NOT_FOUND,
        
        // 입력 검증 오류
        REQUIRED_PARAMETER_MISSING,
        INVALID_PARAMETER_FORMAT,
        INVALID_EMAIL_FORMAT,
        INVALID_USERNAME_FORMAT,
        INVALID_PASSWORD_FORMAT,
        INVALID_GUID_FORMAT,
        INVALID_DATE_FORMAT,
        INVALID_JSON_FORMAT,
        
        // 비즈니스 로직 오류
        GUEST_ID_INVALID,
        PROVIDER_USER_ID_INVALID,
        SESSION_EXPIRED,
        RATE_LIMIT_EXCEEDED,
        RESOURCE_QUOTA_EXCEEDED,
        
        // 크래딧 관련 오류
        INSUFFICIENT_CREDIT_BALANCE,
        CREDIT_TRANSACTION_FAILED,
        CREDIT_GRANT_FAILED
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
            { ErrorCode.USER_CREATION_FAILED, "사용자 생성에 실패했습니다" },
            { ErrorCode.USER_UPDATE_FAILED, "사용자 정보 업데이트에 실패했습니다" },
            
            // 캐릭터 관련 오류
            { ErrorCode.CHARACTER_NOT_FOUND, "캐릭터를 찾을 수 없습니다" },
            { ErrorCode.CHARACTER_ALREADY_EXISTS, "이미 존재하는 캐릭터입니다" },
            { ErrorCode.CHARACTER_CREATION_FAILED, "캐릭터 생성에 실패했습니다" },
            { ErrorCode.CHARACTER_UPDATE_FAILED, "캐릭터 정보 업데이트에 실패했습니다" },
            
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
            { ErrorCode.TOKEN_MISSING, "토큰이 누락되었습니다" },
            { ErrorCode.TOKEN_REFRESH_FAILED, "토큰 갱신에 실패했습니다" },
            { ErrorCode.TOKEN_REVOKE_FAILED, "토큰 폐기에 실패했습니다" },
            { ErrorCode.INVALID_SESSION_ID, "유효하지 않은 세션 ID입니다" },
            
            // OAuth2 관련 오류
            { ErrorCode.OAUTH2_PROVIDER_NOT_SUPPORTED, "지원하지 않는 OAuth2 제공자입니다" },
            { ErrorCode.OAUTH2_PROVIDER_NOT_CONFIGURED, "OAuth2 제공자가 구성되지 않았습니다" },
            { ErrorCode.OAUTH2_CLIENT_ID_INVALID, "유효하지 않은 OAuth2 클라이언트 ID입니다" },
            { ErrorCode.OAUTH2_CLIENT_SECRET_INVALID, "유효하지 않은 OAuth2 클라이언트 시크릿입니다" },
            { ErrorCode.OAUTH2_REDIRECT_URI_INVALID, "유효하지 않은 OAuth2 리다이렉트 URI입니다" },
            { ErrorCode.OAUTH2_AUTHORIZATION_CODE_INVALID, "유효하지 않은 OAuth2 인증 코드입니다" },
            { ErrorCode.OAUTH2_STATE_INVALID, "유효하지 않은 OAuth2 상태값입니다" },
            { ErrorCode.OAUTH2_PKCE_INVALID, "유효하지 않은 PKCE 파라미터입니다" },
            { ErrorCode.OAUTH2_TOKEN_EXCHANGE_FAILED, "OAuth2 토큰 교환에 실패했습니다" },
            { ErrorCode.OAUTH2_USER_INFO_FAILED, "OAuth2 사용자 정보 조회에 실패했습니다" },
            { ErrorCode.OAUTH2_CALLBACK_FAILED, "OAuth2 콜백 처리에 실패했습니다" },
            { ErrorCode.OAUTH2_REQUEST_EXPIRED, "OAuth2 요청이 만료되었습니다" },
            { ErrorCode.OAUTH2_REQUEST_NOT_FOUND, "OAuth2 요청을 찾을 수 없습니다" },
            
            // 입력 검증 오류
            { ErrorCode.REQUIRED_PARAMETER_MISSING, "필수 파라미터가 누락되었습니다" },
            { ErrorCode.INVALID_PARAMETER_FORMAT, "파라미터 형식이 올바르지 않습니다" },
            { ErrorCode.INVALID_EMAIL_FORMAT, "이메일 형식이 올바르지 않습니다" },
            { ErrorCode.INVALID_USERNAME_FORMAT, "사용자명 형식이 올바르지 않습니다" },
            { ErrorCode.INVALID_PASSWORD_FORMAT, "비밀번호 형식이 올바르지 않습니다" },
            { ErrorCode.INVALID_GUID_FORMAT, "GUID 형식이 올바르지 않습니다" },
            { ErrorCode.INVALID_DATE_FORMAT, "날짜 형식이 올바르지 않습니다" },
            { ErrorCode.INVALID_JSON_FORMAT, "JSON 형식이 올바르지 않습니다" },
            
            // 비즈니스 로직 오류
            { ErrorCode.GUEST_ID_INVALID, "유효하지 않은 게스트 ID입니다" },
            { ErrorCode.PROVIDER_USER_ID_INVALID, "유효하지 않은 제공자 사용자 ID입니다" },
            { ErrorCode.SESSION_EXPIRED, "세션이 만료되었습니다" },
            { ErrorCode.RATE_LIMIT_EXCEEDED, "요청 한도를 초과했습니다" },
            { ErrorCode.RESOURCE_QUOTA_EXCEEDED, "리소스 할당량을 초과했습니다" },
            
            // 크래딧 관련 오류
            { ErrorCode.INSUFFICIENT_CREDIT_BALANCE, "크래딧 잔액이 부족합니다" },
            { ErrorCode.CREDIT_TRANSACTION_FAILED, "크래딧 거래에 실패했습니다" },
            { ErrorCode.CREDIT_GRANT_FAILED, "크래딧 지급에 실패했습니다" }
        };

        public static string GetMessage(this ErrorCode errorCode)
        {
            return _errorMessages.TryGetValue(errorCode, out var message) ? message : ErrorCode.INTERNAL_SERVER_ERROR.GetMessage();
        }

        public static string GetCode(this ErrorCode errorCode)
        {
            return errorCode.ToString();
        }
    }
} 