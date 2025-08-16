using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ProjectVG.Api.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IWebHostEnvironment _environment;

        /// <summary>
        /// GlobalExceptionHandler 미들웨어의 인스턴스를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 주어진 요청 파이프라인의 다음 델리게이트, 로거 및 호스팅 환경을 내부 필드에 저장하여
        /// 미들웨어가 예외를 처리하고 환경에 따라 응답을 구성할 수 있도록 초기화합니다.
        /// </remarks>
        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// 파이프라인에서 다음 미들웨어를 비동기적으로 실행하고, 실행 중 발생한 모든 예외를 포착하여 중앙 예외 처리기로 위임합니다.
        /// </summary>
        /// <param name="context">현재 HTTP 요청/응답 컨텍스트.</param>
        /// <returns>미들웨어 호출과 예외 처리가 완료될 때까지 진행되는 비동기 작업.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try {
                await _next(context);
            }
            catch (Exception ex) {
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// 캡처된 예외를 ErrorResponse로 변환하여 JSON으로 직렬화하고 HTTP 응답으로 작성합니다.
        /// </summary>
        /// <param name="context">현재 HTTP 요청/응답 컨텍스트. 응답이 아직 시작되지 않은 경우 본 메서드가 Content-Type 및 StatusCode를 설정합니다.</param>
        /// <param name="exception">처리할 예외 객체.</param>
        /// <returns>응답 작성이 완료될 때까지 완료되는 비동기 작업.</returns>
        /// <remarks>
        /// 생성된 에러 페이로드는 camelCase 속성 명명 규칙으로 직렬화되며, 개발 환경에서는 들여쓰기가 적용됩니다.
        /// 응답은 JSON 문자열로 HTTP 본문에 기록됩니다.
        /// </remarks>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var errorResponse = CreateErrorResponse(exception, context);

            if (!context.Response.HasStarted) {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = errorResponse.StatusCode;
            }

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        /// <summary>
        /// 전달된 예외의 타입을 검사하여 적절한 ErrorResponse 생성기(핸들러)를 호출하고 그 결과를 반환합니다.
        /// </summary>
        /// <param name="exception">처리할 예외 객체.</param>
        /// <param name="context">현재 요청의 HttpContext(응답 상태·추적 식별자 등 핸들러에서 사용).</param>
        /// <returns>예외에 대응하는 ErrorResponse 객체(알려진 타입이면 해당 타입에 맞는 상세 응답, 그렇지 않으면 일반 내부 서버 오류 응답).</returns>
        private ErrorResponse CreateErrorResponse(Exception exception, HttpContext context)
        {
            if (exception is ProjectVG.Common.Exceptions.ValidationException validationEx) {
                return HandleValidationException(validationEx, context);
            }

            if (exception is NotFoundException notFoundEx) {
                return HandleNotFoundException(notFoundEx, context);
            }

            if (exception is ProjectVGException projectVGEx) {
                return HandleProjectVGException(projectVGEx, context);
            }

            if (exception is ExternalServiceException externalEx) {
                return HandleExternalServiceException(externalEx, context);
            }

            if (exception is DbUpdateException dbUpdateEx) {
                return HandleDbUpdateException(dbUpdateEx, context);
            }

            if (exception is KeyNotFoundException keyNotFoundEx) {
                return HandleKeyNotFoundException(keyNotFoundEx, context);
            }

            if (exception is ArgumentException argumentEx) {
                return HandleArgumentException(argumentEx, context);
            }

            if (exception is InvalidOperationException invalidOpEx) {
                return HandleInvalidOperationException(invalidOpEx, context);
            }

            if (exception is UnauthorizedAccessException unauthorizedEx) {
                return HandleUnauthorizedAccessException(unauthorizedEx, context);
            }

            if (exception is TimeoutException timeoutEx) {
                return HandleTimeoutException(timeoutEx, context);
            }

            if (exception is HttpRequestException httpEx) {
                return HandleHttpRequestException(httpEx, context);
            }

            return HandleGenericException(exception, context);
        }

        /// <summary>
        /// 유효성 검사 예외(ValidationException)를 처리하고 클라이언트에 반환할 ErrorResponse 객체를 생성합니다.
        /// </summary>
        /// <remarks>
        /// 예외에 포함된 ValidationErrors 항목에서 각 오류 메시지를 추출하여 Details 리스트로 설정하고, 현재 요청의 TraceIdentifier와 UTC 타임스탬프를 함께 포함합니다. 처리 과정에서 경고 수준으로 로깅됩니다.
        /// </remarks>
        /// <returns>에러 코드, 메시지, HTTP 상태 코드, 타임스탬프, TraceId 및 상세 오류 목록(있을 경우)을 포함한 ErrorResponse</returns>
        private ErrorResponse HandleValidationException(ValidationException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "유효성 검사 예외 발생: {ErrorCode} - {Message}", exception.ErrorCode.ToString(), exception.Message);

            var details = exception.ValidationErrors?.Select(e => e.ErrorMessage ?? string.Empty).ToList();

            return new ErrorResponse {
                ErrorCode = exception.ErrorCode.ToString(),
                Message = exception.Message,
                StatusCode = exception.StatusCode,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier,
                Details = details
            };
        }

        /// <summary>
        /// NotFoundException을 처리하여 표준화된 ErrorResponse를 생성한다.
        /// </summary>
        /// <param name="exception">발생한 NotFoundException — 응답에 사용할 오류 코드와 메시지, HTTP 상태를 포함한다.</param>
        /// <param name="context">현재 HttpContext — 응답의 TraceId를 추출하는 데 사용된다.</param>
        /// <returns>예외 정보(오류 코드, 메시지, 상태 코드, UTC 타임스탬프, TraceId)를 담은 ErrorResponse 인스턴스.</returns>
        private ErrorResponse HandleNotFoundException(NotFoundException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "리소스를 찾을 수 없음: {ErrorCode} - {Message}", exception.ErrorCode.ToString(), exception.Message);

            return new ErrorResponse {
                ErrorCode = exception.ErrorCode.ToString(),
                Message = exception.Message,
                StatusCode = exception.StatusCode,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        /// <summary>
        /// ProjectVGException을 ErrorResponse로 변환한다.
        /// </summary>
        /// <remarks>
        /// 예외의 ErrorCode, Message, StatusCode를 ErrorResponse에 담아 반환하며 응답 추적을 위해 <see cref="HttpContext.TraceIdentifier"/>를 TraceId로 설정한다.
        /// 처리 과정에서 경고 레벨로 로그를 남긴다.
        /// 타임스탬프는 UTC 기준으로 설정된다.
        /// </remarks>
        /// <param name="exception">변환할 ProjectVGException (내부에 ErrorCode 및 StatusCode 포함).</param>
        /// <param name="context">응답에 포함할 TraceIdentifier를 가져오기 위한 HttpContext.</param>
        /// <returns>ErrorResponse 객체(정상적으로 변환된 에러 코드·메시지·상태 코드·타임스탬프·트레이스 ID 포함).</returns>
        private ErrorResponse HandleProjectVGException(ProjectVGException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "ProjectVG 예외 발생: {ErrorCode} - {Message}", exception.ErrorCode.ToString(), exception.Message);

            return new ErrorResponse {
                ErrorCode = exception.ErrorCode.ToString(),
                Message = exception.Message,
                StatusCode = exception.StatusCode,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        /// <summary>
        /// ExternalServiceException을 ErrorResponse로 변환한다.
        /// </summary>
        /// <remarks>
        /// 예외의 ErrorCode, Message, StatusCode를 그대로 사용하고 응답에 UTC 타임스탬프와 요청의 TraceIdentifier를 포함한다.
        /// 또한 내부적으로 예외 정보를 로깅한다.
        /// </remarks>
        /// <param name="context">응답에 포함할 TraceId를 얻기 위해 사용하는 현재 HttpContext.</param>
        /// <returns>클라이언트에 반환할 ErrorResponse 객체.</returns>
        private ErrorResponse HandleExternalServiceException(ExternalServiceException exception, HttpContext context)
        {
            _logger.LogError(exception, "외부 서비스 오류 발생: {ServiceName} - {Endpoint} - {Message}",
                exception.ServiceName, exception.Endpoint, exception.Message);

            return new ErrorResponse {
                ErrorCode = exception.ErrorCode.ToString(),
                Message = exception.Message,
                StatusCode = exception.StatusCode,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        /// <summary>
        /// DbUpdateException을 검사하여 적절한 ErrorResponse로 매핑합니다.
        /// </summary>
        /// <remarks>
        /// 내부 예외 메시지를 소문자 비교로 검사하여 중복 키(RESOURCE_CONFLICT, 409),
        /// 외래 키/제약 조건 위반(CONSTRAINT_VIOLATION, 400)을 우선 처리하고,
        /// 그 외의 DB 업데이트 오류는 일반 데이터베이스 오류(DATABASE_ERROR, 500)로 처리합니다.
        /// 각 응답에는 타임스탬프와 HttpContext.TraceIdentifier가 포함됩니다.
        /// 해당 처리는 내부 예외 메시지 문자열에 의존하므로 메시지 내용에 따라 결과가 달라질 수 있습니다.
        /// 또한 처리 시 적절한 수준으로 로깅(중복/제약: Warning, 기타: Error)을 수행합니다.
        /// </remarks>
        /// <param name="exception">처리할 DbUpdateException.</param>
        /// <param name="context">응답에 포함할 TraceIdentifier를 얻기 위한 HttpContext.</param>
        /// <returns>예외 유형에 대응하는 ErrorResponse(HTTP 상태 코드와 에러 코드 포함).</returns>
        private ErrorResponse HandleDbUpdateException(DbUpdateException exception, HttpContext context)
        {
            var innerMessage = exception.InnerException?.Message?.ToLowerInvariant() ?? string.Empty;

            if (innerMessage.Contains("duplicate") || innerMessage.Contains("unique")) {
                _logger.LogWarning(exception, "데이터베이스 중복 키 오류 발생");
                return new ErrorResponse {
                    ErrorCode = "RESOURCE_CONFLICT",
                    Message = "이미 존재하는 데이터입니다",
                    StatusCode = 409,
                    Timestamp = DateTime.UtcNow,
                    TraceId = context.TraceIdentifier
                };
            }

            if (innerMessage.Contains("foreign key") || innerMessage.Contains("constraint")) {
                _logger.LogWarning(exception, "데이터베이스 제약 조건 위반");
                return new ErrorResponse {
                    ErrorCode = "CONSTRAINT_VIOLATION",
                    Message = "관련 데이터가 존재하여 삭제할 수 없습니다",
                    StatusCode = 400,
                    Timestamp = DateTime.UtcNow,
                    TraceId = context.TraceIdentifier
                };
            }

            _logger.LogError(exception, "데이터베이스 업데이트 오류 발생");
            return new ErrorResponse {
                ErrorCode = "DATABASE_ERROR",
                Message = "데이터베이스 처리 중 오류가 발생했습니다",
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        /// <summary>
        /// KeyNotFoundException을 처리하여 표준화된 ErrorResponse(404)를 생성합니다.
        /// </summary>
        /// <param name="exception">발생한 KeyNotFoundException; 응답의 Message로 사용됩니다.</param>
        /// <param name="context">현재 HttpContext; 응답의 TraceId로 context.TraceIdentifier를 사용합니다.</param>
        /// <returns>HTTP 404 상태와 ErrorCode "RESOURCE_NOT_FOUND", 타임스탬프, TraceId를 포함한 ErrorResponse 인스턴스.</returns>
        private ErrorResponse HandleKeyNotFoundException(KeyNotFoundException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "리소스를 찾을 수 없음: {Message}", exception.Message);

            return new ErrorResponse {
                ErrorCode = "RESOURCE_NOT_FOUND",
                Message = exception.Message,
                StatusCode = (int)HttpStatusCode.NotFound,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        /// <summary>
        /// ArgumentException을 처리하고 표준화된 ErrorResponse(HTTP 400)를 생성합니다.
        /// </summary>
        /// <returns>
        /// ErrorResponse 인스턴스.
        /// - ErrorCode: "INVALID_ARGUMENT"
        /// - Message: "잘못된 요청 파라미터입니다"
        /// - StatusCode: 400
        /// - TraceId: HttpContext.TraceIdentifier
        /// - Details: 개발 환경일 경우 예외 메시지를 단일 항목으로 포함, 그렇지 않으면 null
        /// </returns>
        private ErrorResponse HandleArgumentException(ArgumentException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "잘못된 인수: {Message}", exception.Message);

            return new ErrorResponse {
                ErrorCode = "INVALID_ARGUMENT",
                Message = "잘못된 요청 파라미터입니다",
                StatusCode = (int)HttpStatusCode.BadRequest,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier,
                Details = _environment.IsDevelopment() ? new List<string> { exception.Message } : null
            };
        }

        /// <summary>
        /// InvalidOperationException을 받아 표준화된 ErrorResponse 객체를 생성합니다.
        /// 개발 환경일 경우 예외 메시지를 Details에 포함하고, 모든 응답에 TraceIdentifier를 삽입합니다.
        /// </summary>
        /// <param name="exception">처리할 InvalidOperationException 인스턴스.</param>
        /// <param name="context">현재 HttpContext — 응답에 포함할 TraceIdentifier를 제공.</param>
        /// <returns>INVALID_OPERATION 오류 코드와 HTTP 400 상태를 가진 ErrorResponse (개발 환경에서는 Details에 예외 메시지 포함).</returns>
        private ErrorResponse HandleInvalidOperationException(InvalidOperationException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "잘못된 작업: {Message}", exception.Message);

            return new ErrorResponse {
                ErrorCode = "INVALID_OPERATION",
                Message = "요청한 작업을 수행할 수 없습니다",
                StatusCode = (int)HttpStatusCode.BadRequest,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier,
                Details = _environment.IsDevelopment() ? new List<string> { exception.Message } : null
            };
        }

        /// <summary>
        /// Unauthorized(401) 응답용 ErrorResponse를 생성합니다.
        /// </summary>
        /// <param name="exception">발생한 UnauthorizedAccessException. 응답 메시지는 사용자용으로 일반화됩니다.</param>
        /// <param name="context">현재 HttpContext. 응답에 TraceIdentifier가 포함됩니다.</param>
        /// <returns>
        /// HTTP 401 상태와 "UNAUTHORIZED" 오류 코드를 가진 ErrorResponse 객체를 반환합니다. 반환값에는 UTC 타임스탬프와 요청의 TraceId가 설정됩니다.
        /// </returns>
        private ErrorResponse HandleUnauthorizedAccessException(UnauthorizedAccessException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "권한 없음: {Message}", exception.Message);

            return new ErrorResponse {
                ErrorCode = "UNAUTHORIZED",
                Message = "접근 권한이 없습니다",
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        /// <summary>
        /// Timeout 예외를 처리하여 표준화된 ErrorResponse(HTTP 408, ErrorCode="TIMEOUT")를 생성하고 경고로 로깅합니다.
        /// </summary>
        /// <param name="exception">발생한 <see cref="TimeoutException"/> (메시지는 로그에 포함됨).</param>
        /// <param name="context">응답에 포함할 추적 식별자(TraceId)를 제공하는 현재 <see cref="HttpContext"/>.</param>
        /// <returns>
        /// 생성된 <see cref="ErrorResponse"/> — ErrorCode는 "TIMEOUT", StatusCode는 408(Request Timeout), 메시지는 사용자용 통지 텍스트를 포함합니다.
        /// </returns>
        private ErrorResponse HandleTimeoutException(TimeoutException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "타임아웃 발생: {Message}", exception.Message);

            return new ErrorResponse {
                ErrorCode = "TIMEOUT",
                Message = "요청 처리 시간이 초과되었습니다",
                StatusCode = (int)HttpStatusCode.RequestTimeout,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        /// <summary>
        — HTTP 요청 예외(HttpRequestException)를 처리하여 표준화된 ErrorResponse를 생성합니다.
        /// </summary>
        /// <param name="exception">발생한 HttpRequestException (원인 예외).</param>
        /// <param name="context">응답에 포함할 TraceId를 얻기 위해 사용하는 HttpContext.</param>
        /// <returns>
        /// 외부 서비스 통신 오류를 나타내는 ErrorResponse:
        /// ErrorCode = "HTTP_REQUEST_ERROR", Message는 외부 서비스 통신 실패 안내, StatusCode = 502 (Bad Gateway),
        /// Timestamp는 UTC 현재 시간, TraceId는 context.TraceIdentifier.
        /// </returns>
        private ErrorResponse HandleHttpRequestException(HttpRequestException exception, HttpContext context)
        {
            _logger.LogError(exception, "HTTP 요청 오류: {Message}", exception.Message);

            return new ErrorResponse {
                ErrorCode = "HTTP_REQUEST_ERROR",
                Message = "외부 서비스와의 통신 중 오류가 발생했습니다",
                StatusCode = (int)HttpStatusCode.BadGateway,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        /// <summary>
        /// 예기치 않은 모든 예외를 처리하여 표준화된 ErrorResponse(HTTP 500)를 생성하고 에러를 로깅합니다.
        /// </summary>
        /// <param name="exception">처리할 예외. 개발 환경에서는 메시지와 스택 트레이스를 응답의 Details에 포함합니다.</param>
        /// <param name="context">현재 HTTP 컨텍스트. 응답의 TraceId로 사용됩니다.</param>
        /// <returns>
        /// HTTP 상태 코드 500과 함께 반환할 ErrorResponse 객체.
        /// - ErrorCode: "INTERNAL_SERVER_ERROR"
        /// - Message: 개발 환경이면 예외 메시지, 그렇지 않으면 일반화된 사용자 메시지
        /// - Timestamp: UTC 기준 생성 시각
        /// - TraceId: context.TraceIdentifier
        /// - Details: 개발 환경에서만 예외 타입과 스택 트레이스를 리스트로 포함(그 외에는 null)
        /// </returns>
        private ErrorResponse HandleGenericException(Exception exception, HttpContext context)
        {
            var exceptionType = exception.GetType().Name;
            var isDevelopment = _environment.IsDevelopment();

            _logger.LogError(exception, "예상치 못한 예외 발생: {ExceptionType} - {Message}", exceptionType, exception.Message);

            return new ErrorResponse {
                ErrorCode = "INTERNAL_SERVER_ERROR",
                Message = isDevelopment ? exception.Message : "서버에서 예상치 못한 오류가 발생했습니다",
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier,
                Details = isDevelopment ? new List<string>
                {
                    $"Exception Type: {exceptionType}",
                    $"Stack Trace: {exception.StackTrace}"
                } : null
            };
        }
    }
}
