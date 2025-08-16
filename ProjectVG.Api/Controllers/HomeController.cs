using Microsoft.AspNetCore.Mvc;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class HomeController : ControllerBase
    {
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            return Ok(new
            {
                messageType = "json",
                supportedFormats = new[] { "json", "binary" },
                audioFormat = "wav",
                version = "1.0.0"
            });
        }
        
        [HttpGet("health")]
        [HttpGet("/health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                uptime = Environment.TickCount / 1000.0
            });
        }

        [HttpGet("test-exception/{type}")]
        public IActionResult TestException(string type)
        {
            return type switch
            {
                "notfound" => throw new NotFoundException(ErrorCode.NOT_FOUND, "테스트 리소스를 찾을 수 없습니다"),
                "validation" => throw new ValidationException(ErrorCode.VALIDATION_FAILED, "유효성 검사 실패"),
                "external" => throw new ExternalServiceException("테스트서비스", "http://test.com/api", "외부 서비스 오류", ErrorCode.EXTERNAL_SERVICE_ERROR),
                "generic" => throw new InvalidOperationException("일반적인 시스템 오류가 발생했습니다"),
                "nullref" => throw new NullReferenceException("널 참조 오류가 발생했습니다"),
                "divide" => throw new DivideByZeroException("0으로 나누기 오류가 발생했습니다"),
                _ => throw new ProjectVGException(ErrorCode.BAD_REQUEST, "알 수 없는 예외 타입", 400)
            };
        }
    }
} 