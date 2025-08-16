using Microsoft.AspNetCore.Mvc;
using ProjectVG.Common.Exceptions;
using ProjectVG.Common.Constants;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/v1/test")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        [HttpGet("exceptions/{type}")]
        public IActionResult TestException(string type)
        {
            return type.ToLowerInvariant() switch
            {
                "validation" => throw new ValidationException(ErrorCode.VALIDATION_FAILED, "유효성 검사 실패"),
                "notfound" => throw new NotFoundException(ErrorCode.NOT_FOUND, "리소스를 찾을 수 없습니다"),
                "projectvg" => throw new ProjectVGException(ErrorCode.BAD_REQUEST, "ProjectVG 예외 테스트"),
                "external" => throw new ExternalServiceException("테스트서비스", "http://test.com/api", "외부 서비스 오류", ErrorCode.EXTERNAL_SERVICE_ERROR),
                "argument" => throw new ArgumentException("잘못된 인수입니다"),
                "invalidoperation" => throw new InvalidOperationException("잘못된 작업입니다"),
                "unauthorized" => throw new UnauthorizedAccessException("권한이 없습니다"),
                "timeout" => throw new TimeoutException("타임아웃이 발생했습니다"),
                "httprequest" => throw new HttpRequestException("HTTP 요청 오류"),
                "keynotfound" => throw new KeyNotFoundException("키를 찾을 수 없습니다"),
                "nullref" => throw new NullReferenceException("널 참조 오류"),
                "divide" => throw new DivideByZeroException("0으로 나누기 오류"),
                "index" => throw new IndexOutOfRangeException("인덱스 범위 오류"),
                "format" => throw new FormatException("형식 오류"),
                "overflow" => throw new OverflowException("오버플로우 오류"),
                "notimplemented" => throw new NotImplementedException("구현되지 않은 기능"),
                "notsupported" => throw new NotSupportedException("지원되지 않는 기능"),
                "objectdisposed" => throw new ObjectDisposedException("TestObject", "이미 해제된 객체입니다"),
                "aggregate" => throw new AggregateException(new Exception[] { new ArgumentException("인수 오류"), new InvalidOperationException("작업 오류") }),
                _ => throw new ProjectVGException(ErrorCode.BAD_REQUEST, $"알 수 없는 예외 타입: {type}")
            };
        }

        [HttpGet("db-exceptions/{type}")]
        public IActionResult TestDbException(string type)
        {
            // 실제 DB 예외는 테스트하기 어려우므로 시뮬레이션
            return type.ToLowerInvariant() switch
            {
                "duplicate" => throw new InvalidOperationException("Cannot insert duplicate key row in object 'dbo.Users' with unique index 'IX_Users_Username'"),
                "foreignkey" => throw new InvalidOperationException("The DELETE statement conflicted with the REFERENCE constraint \"FK_Users_Characters\"."),
                "constraint" => throw new InvalidOperationException("The INSERT statement conflicted with the CHECK constraint \"CK_Users_Email\"."),
                _ => throw new InvalidOperationException("데이터베이스 오류 시뮬레이션")
            };
        }

        [HttpGet("performance/{delay}")]
        public async Task<IActionResult> TestPerformance(int delay = 1000)
        {
            await Task.Delay(delay);
            return Ok(new { message = $"지연 시간 {delay}ms 완료", timestamp = DateTime.UtcNow });
        }

        [HttpGet("memory/{size}")]
        public IActionResult TestMemory(int size = 1024)
        {
            var data = new byte[size * 1024]; // KB 단위
            return Ok(new { message = $"{size}KB 메모리 할당 완료", actualSize = data.Length });
        }
    }
}
