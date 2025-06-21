using MainAPI_Server.Models.Reponse;
using MainAPI_Server.Models.Request;
using MainAPI_Server.Services.Session;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MainAPI_Server.Controllers
{
    [ApiController]
    [Route("api/talk")]
    [AllowAnonymous]
    public class TalkController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] TalkRequest request)
        {
            var response = new TalkResponse {
                Id = request.Id,
                Response = request.Message,
            };

            Task.Run(() => ProcessTalkRequestAsync(request));

            return Ok(response);
        }

        private async Task ProcessTalkRequestAsync(TalkRequest request)
        {
            var startTime = DateTime.UtcNow;
            Console.WriteLine($"Talk 요청 처리 시작: 세션ID={request.Id}");

            await SessionManager.SendToClientAsync(request.Id, $"[AI 응답] {request.Message}");

            var endTime = DateTime.UtcNow;
            var processingTime = (endTime - startTime).TotalMilliseconds;
            Console.WriteLine($"Talk 요청 처리 완료, 소요시간: {processingTime:F2}ms");
        }
    }
}
