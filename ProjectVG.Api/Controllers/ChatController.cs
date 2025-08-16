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