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

            var response = new TalkResponse {
                Id = request.Id,
                Response = request.Message,
            };

            return Ok(response);
        }
    }
}
