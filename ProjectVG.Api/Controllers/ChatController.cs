using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Models.API.Request;
using ProjectVG.Application.Services.Chat;
using System.Security.Claims;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/v1/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        /// <summary>
        /// IChatService를 주입 받아 컨트롤러의 채팅 서비스 의존성을 설정합니다.
        /// </summary>
        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// 현재 인증된 사용자로부터 채팅 요청을 받아 채팅 처리를 큐에 등록하고 결과를 반환합니다.
        /// </summary>
        /// <param name="request">클라이언트가 보낸 채팅 요청 객체 (Message, CharacterId 포함).</param>
        /// <returns>큐에 등록된 작업의 결과를 포함한 HTTP 200 응답(IActionResult).</returns>
        /// <exception cref="ValidationException">사용자 식별자(ClaimTypes.NameIdentifier)가 없거나 GUID로 파싱할 수 없을 경우 발생하며, ErrorCode.AUTHENTICATION_FAILED를 포함합니다.</exception>
        [HttpPost]
        [JwtAuthentication]
        public async Task<IActionResult> ProcessChat([FromBody] ChatRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                throw new ValidationException(ErrorCode.AUTHENTICATION_FAILED);
            }

            var command = new ProcessChatCommand
            {
                UserId = userGuid,
                Message = request.Message,
                CharacterId = request.CharacterId
            };

            var result = await _chatService.EnqueueChatRequestAsync(command);
            
            return Ok(result);
        }
    }
} 