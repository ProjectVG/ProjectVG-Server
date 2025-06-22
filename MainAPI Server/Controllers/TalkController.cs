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

        private readonly IMemoryStoreClient _ragClient;

        public TalkController(IMemoryStoreClient ragClient)
        {
            _ragClient = ragClient;
        }

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

            // todo : 요청 메시지를 RAG로 전송
            var ragResult = await _ragClient.SearchAsync(request.Message);

            // todo : 해당 데이터를 LLM 모델로 전송

            // todo : 보이스 서버로 전송

            // todo : 대화 내용 전부 저장
            await _ragClient.AddMemoryAsync(request.Message);

            await SessionManager.SendToClientAsync(request.Id, $"[요청] : {request.Message} [응답] : {ragResult}");

            var endTime = DateTime.UtcNow;
            var processingTime = (endTime - startTime).TotalMilliseconds;
            Console.WriteLine($"Talk 요청 처리 완료, 소요시간: {processingTime:F2}ms");
        }
    }
}
