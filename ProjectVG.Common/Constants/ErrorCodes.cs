namespace ProjectVG.Common.Constants
{

    ///
    /// 에러 코드 작성시 다음과 같은 컨벤션을 따를 것!
    /// [도메인]_[상황]_[결과]
    /// 


    public static class ErrorCodes
    {
        // 일반 에러
        public const string 일반_내부_오류 = "일반_내부_오류";
        public const string 일반_잘못된_요청 = "일반_잘못된_요청";
        public const string 일반_찾을_수_없음 = "일반_찾을_수_없음";
        public const string 일반_충돌 = "일반_충돌";
        public const string 일반_유효성_검사_실패 = "일반_유효성_검사_실패";

        // 인증/인가 관련 에러
        public const string 인증_실패 = "인증_실패";
        public const string 인증_권한_없음 = "인증_권한_없음";
        public const string 인증_토큰_유효하지_않음 = "인증_토큰_유효하지_않음";
        public const string 인증_토큰_만료 = "인증_토큰_만료";
        public const string 인증_권한_부족 = "인증_권한_부족";

        // 사용자 관련 에러
        public const string 사용자_찾을_수_없음 = "사용자_찾을_수_없음";
        public const string 사용자_이미_존재 = "사용자_이미_존재";
        public const string 사용자_이메일_중복 = "사용자_이메일_중복";
        public const string 사용자_사용자명_중복 = "사용자_사용자명_중복";
        public const string 사용자_이메일_유효하지_않음 = "사용자_이메일_유효하지_않음";
        public const string 사용자_사용자명_유효하지_않음 = "사용자_사용자명_유효하지_않음";
        public const string 사용자_비활성화 = "사용자_비활성화";
        public const string 사용자_생성_실패 = "사용자_생성_실패";
        public const string 사용자_수정_실패 = "사용자_수정_실패";
        public const string 사용자_삭제_실패 = "사용자_삭제_실패";

        // 캐릭터 관련 에러
        public const string 캐릭터_찾을_수_없음 = "캐릭터_찾을_수_없음";
        public const string 캐릭터_이미_존재 = "캐릭터_이미_존재";
        public const string 캐릭터_이름_유효하지_않음 = "캐릭터_이름_유효하지_않음";
        public const string 캐릭터_비활성화 = "캐릭터_비활성화";
        public const string 캐릭터_생성_실패 = "캐릭터_생성_실패";
        public const string 캐릭터_수정_실패 = "캐릭터_수정_실패";
        public const string 캐릭터_삭제_실패 = "캐릭터_삭제_실패";

        // 채팅 관련 에러
        public const string 채팅_세션_찾을_수_없음 = "채팅_세션_찾을_수_없음";
        public const string 채팅_메시지_유효하지_않음 = "채팅_메시지_유효하지_않음";
        public const string 채팅_세션_만료 = "채팅_세션_만료";
        public const string 채팅_메시지_전송_실패 = "채팅_메시지_전송_실패";
        public const string 채팅_세션_생성_실패 = "채팅_세션_생성_실패";

        // 외부 서비스 관련 에러
        public const string 외부_서비스_오류 = "외부_서비스_오류";
        public const string 외부_LLM_서비스_오류 = "외부_LLM_서비스_오류";
        public const string 외부_메모리_스토어_오류 = "외부_메모리_스토어_오류";
        public const string 외부_서비스_연결_실패 = "외부_서비스_연결_실패";
        public const string 외부_서비스_시간_초과 = "외부_서비스_시간_초과";

        // 네트워크 관련 에러
        public const string 네트워크_연결_오류 = "네트워크_연결_오류";
        public const string 네트워크_시간_초과 = "네트워크_시간_초과";
        public const string 네트워크_통신_실패 = "네트워크_통신_실패";

        // 리소스 관련 에러
        public const string 리소스_제한_초과 = "리소스_제한_초과";
        public const string 요청_제한_초과 = "요청_제한_초과";
        public const string 할당량_초과 = "할당량_초과";
    }
} 