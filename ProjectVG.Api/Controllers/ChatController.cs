using ProjectVG.Api.Models.Chat;
using ProjectVG.Application.Services.Chat;
using ProjectVG.Application.Models.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [AllowAnonymous]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public IActionResult ProcessChat([FromBody] ChatRequestDto request)
        {
            var command = new ProcessChatCommand
            {
                SessionId = request.SessionId,
                Actor = request.Actor,
                Message = request.Message,
                Action = request.Action,
                CharacterId = request.CharacterId
            };

            Task.Run(() => _chatService.ProcessChatRequestAsync(command));

            return Ok(new { 
                success = true, 
                message = "Chat request processed",
                sessionId = request.SessionId 
            });
        }
    }
} 