using ProjectVG.Api.Models.Chat;
using ProjectVG.Application.Services.Chat;
using ProjectVG.Application.Models.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [AllowAnonymous]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IServiceScopeFactory _scopeFactory;

        public ChatController(IChatService chatService, IServiceScopeFactory scopeFactory)
        {
            _chatService = chatService;
            _scopeFactory = scopeFactory;
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

            Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
                await chatService.ProcessChatRequestAsync(command);
            });

            return Ok(new { 
                success = true, 
                message = "채팅 요청이 처리되었습니다",
                sessionId = request.SessionId 
            });
        }
    }
} 