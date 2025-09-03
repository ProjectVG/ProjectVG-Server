using System.ComponentModel.DataAnnotations;

namespace ProjectVG.Api.Models.Conversation.Request
{
    public class GetConversationHistoryRequest
    {
        /// <summary>
        /// 페이지 번호 (1부터 시작)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// 페이지 크기
        /// </summary>
        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 10;
    }
}