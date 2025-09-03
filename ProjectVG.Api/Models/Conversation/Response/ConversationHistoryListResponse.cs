namespace ProjectVG.Api.Models.Conversation.Response
{
    public class ConversationHistoryListResponse
    {
        /// <summary>
        /// 대화 기록 목록
        /// </summary>
        public IEnumerable<ConversationHistoryResponse> Messages { get; set; } = new List<ConversationHistoryResponse>();

        /// <summary>
        /// 총 메시지 수
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 현재 페이지
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// 페이지 크기
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 총 페이지 수
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 다음 페이지 존재 여부
        /// </summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// 이전 페이지 존재 여부
        /// </summary>
        public bool HasPreviousPage { get; set; }
    }
}