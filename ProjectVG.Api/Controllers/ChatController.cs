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

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        [JwtAuthentication]
        public async Task<IActionResult> ProcessChat([FromBody] ChatRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                throw new ValidationException(ErrorCode.AUTHENTICATION_FAILED);
            }

            var command = new ChatRequestCommand
            {
                UserId = userGuid,
                UserPrompt = request.Message,
                CharacterId = request.CharacterId
            };

            var result = await _chatService.EnqueueChatRequestAsync(command);
            
            return Ok(result);
        }
    }
} 