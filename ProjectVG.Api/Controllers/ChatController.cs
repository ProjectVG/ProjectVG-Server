using ProjectVG.Application.Models.API.Request;
using ProjectVG.Application.Services.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/v1/chat")]
    [AllowAnonymous]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, IServiceScopeFactory scopeFactory, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// 클라이언트로부터 받은 채팅 요청을 처리 명령으로 변환하여 비동기 큐에 등록하고, 등록 결과 메타정보를 반환합니다.
        /// </summary>
        /// <param name="request">요청 본문으로 전달된 채팅 요청 모델(처리할 메시지와 관련 메타데이터를 포함).</param>
        /// <returns>HTTP 200 OK와 함께 JSON 페이로드를 반환합니다. 페이로드 필드는 다음을 포함합니다:
        /// <list type="bullet">
        /// <item><c>success</c>: 항상 true (요청이 정상적으로 큐에 등록되었음을 나타냄).</item>
        /// <item><c>status</c>: 큐 등록 결과 상태 코드 또는 상태 문자열.</item>
        /// <item><c>message</c>: 상태에 대한 설명 메시지.</item>
        /// <item><c>sessionId</c>: 생성되거나 할당된 세션 식별자.</item>
        /// <item><c>userId</c>: 요청을 보낸 사용자 식별자(있을 경우).</item>
        /// <item><c>characterId</c>: 관련 캐릭터 식별자(있을 경우).</item>
        /// <item><c>requestedAt</c>: 요청이 등록된 시각(타임스탬프).</item>
        /// </list>
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> ProcessChat([FromBody] ChatRequest request)
        {
            var command = request.ToProcessChatCommand();
            var requestResponse = await _chatService.EnqueueChatRequestAsync(command);

            return Ok(new { 
                success = true, 
                status = requestResponse.Status,
                message = requestResponse.Message,
                sessionId = requestResponse.SessionId,
                userId = requestResponse.UserId,
                characterId = requestResponse.CharacterId,
                requestedAt = requestResponse.RequestedAt
            });
        }
    }
} 