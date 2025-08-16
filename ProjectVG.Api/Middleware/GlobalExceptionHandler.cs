using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using ProjectVG.Common.Exceptions;

namespace ProjectVG.Api.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var errorResponse = CreateErrorResponse(exception, context);
            
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = errorResponse.StatusCode;

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private ErrorResponse CreateErrorResponse(Exception exception, HttpContext context)
        {
            if (exception is ProjectVG.Common.Exceptions.ValidationException validationEx)
            {
                return HandleValidationException(validationEx, context);
            }
            
            if (exception is NotFoundException notFoundEx)
            {
                return HandleNotFoundException(notFoundEx, context);
            }
            
            if (exception is ProjectVGException projectVGEx)
            {
                return HandleProjectVGException(projectVGEx, context);
            }
            
            if (exception is ExternalServiceException externalEx)
            {
                return HandleExternalServiceException(externalEx, context);
            }
            
            if (exception is DbUpdateException dbUpdateEx)
            {
                return HandleDbUpdateException(dbUpdateEx, context);
            }
            
            if (exception is KeyNotFoundException keyNotFoundEx)
            {
                return HandleKeyNotFoundException(keyNotFoundEx, context);
            }
            
            if (exception is ArgumentException argumentEx)
            {
                return HandleArgumentException(argumentEx, context);
            }
            
            if (exception is InvalidOperationException invalidOpEx)
            {
                return HandleInvalidOperationException(invalidOpEx, context);
            }
            
            if (exception is UnauthorizedAccessException unauthorizedEx)
            {
                return HandleUnauthorizedAccessException(unauthorizedEx, context);
            }
            
            if (exception is TimeoutException timeoutEx)
            {
                return HandleTimeoutException(timeoutEx, context);
            }
            
            if (exception is HttpRequestException httpEx)
            {
                return HandleHttpRequestException(httpEx, context);
            }
            
            return HandleGenericException(exception, context);
        }

        private ErrorResponse HandleValidationException(ValidationException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "유효성 검사 예외 발생: {ErrorCode} - {Message}", exception.ErrorCode.ToString(), exception.Message);
            
            var details = exception.ValidationErrors?.Select(e => e.ErrorMessage ?? string.Empty).ToList();
            
            return new ErrorResponse
            {
                ErrorCode = exception.ErrorCode.ToString(),
                Message = exception.Message,
                StatusCode = exception.StatusCode,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier,
                Details = details
            };
        }

        private ErrorResponse HandleNotFoundException(NotFoundException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "리소스를 찾을 수 없음: {ErrorCode} - {Message}", exception.ErrorCode.ToString(), exception.Message);
            
            return new ErrorResponse
            {
                ErrorCode = exception.ErrorCode.ToString(),
                Message = exception.Message,
                StatusCode = exception.StatusCode,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        private ErrorResponse HandleProjectVGException(ProjectVGException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "ProjectVG 예외 발생: {ErrorCode} - {Message}", exception.ErrorCode.ToString(), exception.Message);
            
            return new ErrorResponse
            {
                ErrorCode = exception.ErrorCode.ToString(),
                Message = exception.Message,
                StatusCode = exception.StatusCode,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        private ErrorResponse HandleExternalServiceException(ExternalServiceException exception, HttpContext context)
        {
            _logger.LogError(exception, "외부 서비스 오류 발생: {ServiceName} - {Endpoint} - {Message}", 
                exception.ServiceName, exception.Endpoint, exception.Message);
            
            return new ErrorResponse
            {
                ErrorCode = exception.ErrorCode.ToString(),
                Message = exception.Message,
                StatusCode = exception.StatusCode,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        private ErrorResponse HandleDbUpdateException(DbUpdateException exception, HttpContext context)
        {
            var innerMessage = exception.InnerException?.Message?.ToLowerInvariant() ?? string.Empty;
            
            if (innerMessage.Contains("duplicate") || innerMessage.Contains("unique"))
            {
                _logger.LogWarning(exception, "데이터베이스 중복 키 오류 발생");
                return new ErrorResponse
                {
                    ErrorCode = "RESOURCE_CONFLICT",
                    Message = "이미 존재하는 데이터입니다",
                    StatusCode = 409,
                    Timestamp = DateTime.UtcNow,
                    TraceId = context.TraceIdentifier
                };
            }
            
            if (innerMessage.Contains("foreign key") || innerMessage.Contains("constraint"))
            {
                _logger.LogWarning(exception, "데이터베이스 제약 조건 위반");
                return new ErrorResponse
                {
                    ErrorCode = "CONSTRAINT_VIOLATION",
                    Message = "관련 데이터가 존재하여 삭제할 수 없습니다",
                    StatusCode = 400,
                    Timestamp = DateTime.UtcNow,
                    TraceId = context.TraceIdentifier
                };
            }
            
            _logger.LogError(exception, "데이터베이스 업데이트 오류 발생");
            return new ErrorResponse
            {
                ErrorCode = "DATABASE_ERROR",
                Message = "데이터베이스 처리 중 오류가 발생했습니다",
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        private ErrorResponse HandleKeyNotFoundException(KeyNotFoundException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "리소스를 찾을 수 없음: {Message}", exception.Message);
            
            return new ErrorResponse
            {
                ErrorCode = "RESOURCE_NOT_FOUND",
                Message = exception.Message,
                StatusCode = (int)HttpStatusCode.NotFound,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        private ErrorResponse HandleArgumentException(ArgumentException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "잘못된 인수: {Message}", exception.Message);
            
            return new ErrorResponse
            {
                ErrorCode = "INVALID_ARGUMENT",
                Message = "잘못된 요청 파라미터입니다",
                StatusCode = (int)HttpStatusCode.BadRequest,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier,
                Details = _environment.IsDevelopment() ? new List<string> { exception.Message } : null
            };
        }

        private ErrorResponse HandleInvalidOperationException(InvalidOperationException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "잘못된 작업: {Message}", exception.Message);
            
            return new ErrorResponse
            {
                ErrorCode = "INVALID_OPERATION",
                Message = "요청한 작업을 수행할 수 없습니다",
                StatusCode = (int)HttpStatusCode.BadRequest,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier,
                Details = _environment.IsDevelopment() ? new List<string> { exception.Message } : null
            };
        }

        private ErrorResponse HandleUnauthorizedAccessException(UnauthorizedAccessException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "권한 없음: {Message}", exception.Message);
            
            return new ErrorResponse
            {
                ErrorCode = "UNAUTHORIZED",
                Message = "접근 권한이 없습니다",
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        private ErrorResponse HandleTimeoutException(TimeoutException exception, HttpContext context)
        {
            _logger.LogWarning(exception, "타임아웃 발생: {Message}", exception.Message);
            
            return new ErrorResponse
            {
                ErrorCode = "TIMEOUT",
                Message = "요청 처리 시간이 초과되었습니다",
                StatusCode = (int)HttpStatusCode.RequestTimeout,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        private ErrorResponse HandleHttpRequestException(HttpRequestException exception, HttpContext context)
        {
            _logger.LogError(exception, "HTTP 요청 오류: {Message}", exception.Message);
            
            return new ErrorResponse
            {
                ErrorCode = "HTTP_REQUEST_ERROR",
                Message = "외부 서비스와의 통신 중 오류가 발생했습니다",
                StatusCode = (int)HttpStatusCode.BadGateway,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        private ErrorResponse HandleGenericException(Exception exception, HttpContext context)
        {
            var exceptionType = exception.GetType().Name;
            var isDevelopment = _environment.IsDevelopment();
            
            _logger.LogError(exception, "예상치 못한 예외 발생: {ExceptionType} - {Message}", exceptionType, exception.Message);
            
            return new ErrorResponse
            {
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
