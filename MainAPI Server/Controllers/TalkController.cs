using MainAPI_Server.Models.Reponse;
using MainAPI_Server.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace MainAPI_Server.Controllers
{
    [ApiController]
    [Route("api/talk")]
    public class TalkController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] TalkRequest request)
        {

            // todo : 작업을 실재 비동기로 실행
            Task.Run(() => {
                ProcessTalkRequestAsync(request);
            });

            // 아래는 요청이 잘 도착했다는 메시지만 전송
            var response = new TalkResponse {
                Id = request.Id,
                Response = request.Message,
            };

            return Ok(response);
        }

        private async Task ProcessTalkRequestAsync(TalkRequest request)
        {
            {
                Console.WriteLine("작업 실행");
            }
        }
    }
}