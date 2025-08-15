using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ProjectVG.Api.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
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
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private ErrorResponse CreateErrorResponse(Exception exception, HttpContext context)
        {
            if (exception is ValidationException validationEx)
            {
                return HandleValidationException(validationEx, context);
            }
            
            
            
            if (exception is ProjectVGException projectVGEx)
            {
                return HandleProjectVGException(projectVGEx, context);
            }
            
            if (exception is DbUpdateException dbUpdateEx)
            {
                return HandleDbUpdateException(dbUpdateEx, context);
            }
            
            if (exception is KeyNotFoundException keyNotFoundEx)
            {
                return HandleKeyNotFoundException(keyNotFoundEx, context);
            }
            
            return HandleGenericException(exception, context);
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

        

        private ErrorResponse HandleDbUpdateException(DbUpdateException exception, HttpContext context)
        {
            if (exception.InnerException?.Message.Contains("duplicate") == true)
            {
                _logger.LogWarning(exception, "데이터베이스 중복 키 오류 발생");
                return new ErrorResponse
                {
                    ErrorCode = "리소스_중복",
                    Message = "이미 존재하는 데이터입니다",
                    StatusCode = 409,
                    Timestamp = DateTime.UtcNow,
                    TraceId = context.TraceIdentifier
                };
            }
            
            _logger.LogError(exception, "데이터베이스 업데이트 오류 발생");
            return new ErrorResponse
            {
                ErrorCode = "데이터베이스_오류",
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
                ErrorCode = "리소스_찾을_수_없음",
                Message = exception.Message,
                StatusCode = (int)HttpStatusCode.NotFound,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }

        private ErrorResponse HandleGenericException(Exception exception, HttpContext context)
        {
            _logger.LogError(exception, "예상치 못한 예외 발생: {Message}", exception.Message);
            
            return new ErrorResponse
            {
                ErrorCode = "내부_서버_오류",
                Message = "서버에서 예상치 못한 오류가 발생했습니다",
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier
            };
        }
    }
}
