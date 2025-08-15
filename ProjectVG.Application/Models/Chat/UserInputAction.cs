namespace ProjectVG.Application.Models.Chat
{
    public enum UserInputAction
    {
        /// <summary>
        /// 무시 - 아무런 행동도 하지 않는다. 즉시 프로세스를 종료하고 HTTP를 반환한다.
        /// </summary>
        Ignore = 0,
        
        /// <summary>
        /// 거절 - 작업을 거절한다. 해당 답변은 클라이언트에 캐시된 대화를 사용한다. 대화 내용을 저장하고 프로세스를 종료한다.
        /// </summary>
        Reject = 1,
        
        /// <summary>
        /// 대화 - 정상적인 일반 대화, 그냥 처리한다.
        /// </summary>
        Chat = 3,
        
        /// <summary>
        /// 미정 - 추후 기능 추가 시 지정 (이미지 처리, 웹 검색 등이 될 수 있음)
        /// </summary>
        Undefined = 4
    }
}
