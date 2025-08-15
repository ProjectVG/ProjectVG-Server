using ProjectVG.Api.Models.Chat;
using ProjectVG.Application.Services.Chat;
using ProjectVG.Application.Models.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        [HttpPost]
        public async Task<IActionResult> ProcessChat([FromBody] ChatRequest request)
        {
            // 요청 데이터 로깅
            _logger.LogInformation("채팅 요청 데이터: SessionId={SessionId}, UserId={UserId}, CharacterId={CharacterId}, Message={Message}", 
                request.SessionId, request.UserId, request.CharacterId, request.Message);

            var command = request.ToProcessChatCommand();

            var requestResponse = await _chatService.EnqueueChatRequestAsync(command);

            if (!requestResponse.IsAccepted)
            {
                _logger.LogWarning("채팅 요청 거부: {Message}", requestResponse.Message);
                return BadRequest(new { 
                    success = false, 
                    status = requestResponse.Status,
                    message = requestResponse.Message,
                    errorCode = requestResponse.ErrorCode,
                    requestedAt = requestResponse.RequestedAt
                });
            }

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