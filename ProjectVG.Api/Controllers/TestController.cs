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

        /// <summary>
        /// TestController의 새 인스턴스를 초기화합니다. 전달된 로거를 내부 필드에 저장합니다.
        /// </summary>
        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 요청된 타입에 따라 다양한 예외를 발생시켜 예외 처리 동작을 테스트합니다.
        /// </summary>
        /// <remarks>
        /// 입력된 <c>type</c> 문자열(소문자화)에 따라 특정 예외를 즉시 던집니다.
        /// 예: "validation", "notfound", "projectvg", "external", "argument", "timeout", "aggregate" 등.
        /// 이 메서드는 정상적으로 응답을 반환하지 않으며, 테스트용으로 의도적으로 예외를 발생시킵니다.
        /// </remarks>
        /// <param name="type">발생시킬 예외의 식별자(예: "validation", "notfound", "timeout" 등).</param>
        /// <returns>정상 반환하지 않으며, 입력된 타입에 매핑된 예외(예: ValidationException, NotFoundException, ProjectVGException 등)를 던집니다.</returns>
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

        /// <summary>
        /// 데이터베이스 관련 예외를 시뮬레이션하기 위해 지정한 유형에 따라 즉시 예외를 던집니다.
        /// </summary>
        /// <param name="type">시뮬레이션할 예외 유형. 허용값: "duplicate" (중복 키), "foreignkey" (외래키 제약 위반), "constraint" (체크 제약 위반). 대소문자 구분 없음.</param>
        /// <exception cref="InvalidOperationException">
        /// 항상 던져집니다. 전달된 <paramref name="type"/>에 따라 다음과 같은 메시지를 갖는 InvalidOperationException이 발생합니다:
        /// - "duplicate": 중복 키 삽입 시나리오 메시지
        /// - "foreignkey": 외래키 제약 위반 메시지
        /// - "constraint": 체크 제약 위반 메시지
        /// - 기타: 일반적인 데이터베이스 오류 시뮬레이션 메시지
        /// </exception>
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

        /// <summary>
        /// 지정한 밀리초만큼 비동기 지연 후 성공 응답(JSON)을 반환합니다.
        /// </summary>
        /// <param name="delay">대기 시간(밀리초). 기본값 1000.</param>
        /// <returns>지연이 완료된 시점의 메시지와 UTC 타임스탬프를 포함한 200 OK 응답(IActionResult).</returns>
        [HttpGet("performance/{delay}")]
        public async Task<IActionResult> TestPerformance(int delay = 1000)
        {
            await Task.Delay(delay);
            return Ok(new { message = $"지연 시간 {delay}ms 완료", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// 지정된 크기(킬로바이트)만큼 바이트 배열을 할당하고 성공 응답을 반환합니다.
        /// </summary>
        /// <param name="size">할당할 크기(단위: KB). 기본값은 1024KB입니다.</param>
        /// <returns>할당 완료 메시지와 실제 할당된 바이트 수를 포함한 200 OK 응답을 반환합니다.</returns>
        [HttpGet("memory/{size}")]
        public IActionResult TestMemory(int size = 1024)
        {
            var data = new byte[size * 1024]; // KB 단위
            return Ok(new { message = $"{size}KB 메모리 할당 완료", actualSize = data.Length });
        }
    }
}
