using ProjectVG.Application.Models.API.Request;
using ProjectVG.Application.Services.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ProjectVG.Api.Filters;
using ProjectVG.Application.Models.Chat;
using System.Security.Claims;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/v1/chat")]
    [AllowAnonymous]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [JwtAuthentication]
        [HttpPost]
        public async Task<IActionResult> ProcessChat([FromBody] ChatRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized(new { success = false, message = "Invalid user information from token" });
            }
          
            ProcessChatCommand command = new() {
                UserId = userGuid,
                CharacterId = request.CharacterId,
                Message = request.Message,
                SessionId = request.SessionId,
            };

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