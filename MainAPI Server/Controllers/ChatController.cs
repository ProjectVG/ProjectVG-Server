using MainAPI_Server.Models.Reponse;
using MainAPI_Server.Models.Request;
using MainAPI_Server.Services.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MainAPI_Server.Controllers
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
        public IActionResult Post([FromBody] ChatRequest request)
        {
            var response = new ChatResponse {
                SessionId = request.SessionId,
                Response = request.Message,
            };

            Task.Run(() => _chatService.ProcessChatRequestAsync(request));

            return Ok(response);
        }
    }
}
