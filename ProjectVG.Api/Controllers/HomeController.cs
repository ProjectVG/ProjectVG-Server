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
        
        /// <summary>
        /// 애플리케이션의 상태 정보를 반환합니다.
        /// </summary>
        /// <returns>
        /// HTTP 200 응답과 함께 현재 상태를 담은 JSON 객체를 반환합니다. 반환 객체의 필드는 다음과 같습니다:
        /// - status: 상태 문자열("healthy")
        /// - timestamp: 응답 시각(UTC)
        /// - version: 애플리케이션 버전("1.0.0")
        /// - uptime: 애플리케이션 가동 시간(초)
        /// </returns>
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

        /// <summary>
        /// 지정한 키워드에 따라 다양한 예외를 발생시켜 예외 처리 경로를 테스트하는 API 엔드포인트입니다.
        /// </summary>
        /// <param name="type">발생시킬 예외 종류를 나타내는 문자열 키워드 (예: "notfound", "validation", "external", "generic", "nullref", "divide").</param>
        /// <remarks>
        /// 매핑:
        /// "notfound"  -> NotFoundException(ErrorCode.NOT_FOUND, "테스트 리소스를 찾을 수 없습니다")
        /// "validation" -> ValidationException(ErrorCode.VALIDATION_FAILED, "유효성 검사 실패")
        /// "external"  -> ExternalServiceException("테스트서비스", "http://test.com/api", "외부 서비스 오류", ErrorCode.EXTERNAL_SERVICE_ERROR)
        /// "generic"   -> InvalidOperationException("일반적인 시스템 오류가 발생했습니다")
        /// "nullref"   -> NullReferenceException("널 참조 오류가 발생했습니다")
        /// "divide"    -> DivideByZeroException("0으로 나누기 오류가 발생했습니다")
        /// 기타 값     -> ProjectVGException(ErrorCode.BAD_REQUEST, "알 수 없는 예외 타입", 400)
        /// 이 액션은 정상적인 성공 응답을 반환하지 않고 항상 예외를 던집니다.
        /// </remarks>
        /// <exception cref="NotFoundException">리소스를 찾을 수 없음을 나타냅니다 (type == "notfound").</exception>
        /// <exception cref="ValidationException">유효성 검사 실패를 나타냅니다 (type == "validation").</exception>
        /// <exception cref="ExternalServiceException">외부 서비스 호출 오류를 나타냅니다 (type == "external").</exception>
        /// <exception cref="InvalidOperationException">일반적인 시스템 오류를 시뮬레이트합니다 (type == "generic").</exception>
        /// <exception cref="NullReferenceException">널 참조 오류를 시뮬레이트합니다 (type == "nullref").</exception>
        /// <exception cref="DivideByZeroException">0으로 나누기 오류를 시뮬레이트합니다 (type == "divide").</exception>
        /// <exception cref="ProjectVGException">알 수 없는 타입에 대해 잘못된 요청을 나타냅니다 (기본 처리).</exception>
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