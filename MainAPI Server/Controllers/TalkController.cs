using MainAPI_Server.Models.Reponse;
using MainAPI_Server.Models.Request;
using MainAPI_Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MainAPI_Server.Controllers
{
    [ApiController]
    [Route("api/talk")]
    [AllowAnonymous]
    public class TalkController : ControllerBase
    {
        private readonly ITalkService _talkService;

        public TalkController(ITalkService talkService)
        {
            _talkService = talkService;
        }

        [HttpPost]
        public IActionResult Post([FromBody] TalkRequest request)
        {
            var response = new TalkResponse {
                Id = request.Id,
                Response = request.Message,
            };

            Task.Run(() => _talkService.ProcessTalkRequestAsync(request));

            return Ok(response);
        }
    }
}
