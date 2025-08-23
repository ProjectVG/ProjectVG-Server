using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("auth")]
    public class OAuthController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly JwtService _jwtService;

        public OAuthController(IHttpClientFactory httpClientFactory, JwtService jwtService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _jwtService = jwtService;
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            return Ok(new { message = "OAuth2 callback endpoint - implementation pending" });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] string userId)
        {
            return Ok(new { message = "OAuth2 refresh endpoint - implementation pending" });
        }
    }
}
