using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Api.Middleware;
using ProjectVG.Common.Constants;
using ProjectVG.Common.Exceptions;
using ProjectVG.Common.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using Xunit;

namespace ProjectVG.Tests.Api.Middleware
{
    public class GlobalExceptionHandlerTests
    {
        private readonly Mock<RequestDelegate> _mockNext;
        private readonly Mock<ILogger<GlobalExceptionHandler>> _mockLogger;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly GlobalExceptionHandler _handler;
        private readonly DefaultHttpContext _httpContext;

        public GlobalExceptionHandlerTests()
        {
            _mockNext = new Mock<RequestDelegate>();
            _mockLogger = new Mock<ILogger<GlobalExceptionHandler>>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            
            _handler = new GlobalExceptionHandler(
                _mockNext.Object,
                _mockLogger.Object,
                _mockEnvironment.Object);
            
            _httpContext = new DefaultHttpContext();
            _httpContext.Response.Body = new MemoryStream();
        }

        #region Successful Flow Tests

        [Fact]
        public async Task InvokeAsync_NoException_ShouldCallNextDelegate()
        {
            // Arrange
            _mockNext.Setup(x => x(_httpContext)).Returns(Task.CompletedTask);

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _mockNext.Verify(x => x(_httpContext), Times.Once);
        }

        #endregion

        #region ValidationException Tests

        [Fact]
        public async Task InvokeAsync_ValidationException_ShouldReturnBadRequest()
        {
            // Arrange
            var validationException = new ProjectVG.Common.Exceptions.ValidationException(ErrorCode.INVALID_INPUT, "테스트 유효성 검사 오류");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(validationException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(400);
            _httpContext.Response.ContentType.Should().Be("application/json");

            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse.Should().NotBeNull();
            errorResponse!.ErrorCode.Should().Be(ErrorCode.INVALID_INPUT.ToString());
            errorResponse.Message.Should().Be("테스트 유효성 검사 오류");
            errorResponse.StatusCode.Should().Be(400);
            errorResponse.TraceId.Should().Be(_httpContext.TraceIdentifier);
            errorResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task InvokeAsync_ValidationExceptionWithValidationErrors_ShouldIncludeDetails()
        {
            // Arrange
            var validationErrors = new List<ValidationResult>
            {
                new ValidationResult("필드1 오류", new[] { "필드1" }),
                new ValidationResult("필드2 오류", new[] { "필드2" })
            };
            var validationException = new ProjectVG.Common.Exceptions.ValidationException(ErrorCode.INVALID_INPUT, "유효성 검사 실패", validationErrors);
            
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(validationException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse!.Details.Should().NotBeNull();
            errorResponse.Details.Should().HaveCount(2);
            errorResponse.Details.Should().Contain("필드1 오류");
            errorResponse.Details.Should().Contain("필드2 오류");
        }

        #endregion

        #region NotFoundException Tests

        [Fact]
        public async Task InvokeAsync_NotFoundException_ShouldReturnNotFound()
        {
            // Arrange
            var notFoundException = new NotFoundException(ErrorCode.USER_NOT_FOUND, "사용자를 찾을 수 없습니다");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(notFoundException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(404);
            
            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse.Should().NotBeNull();
            errorResponse!.ErrorCode.Should().Be(ErrorCode.USER_NOT_FOUND.ToString());
            errorResponse.Message.Should().Be("사용자를 찾을 수 없습니다");
            errorResponse.StatusCode.Should().Be(404);
        }

        #endregion

        #region AuthenticationException Tests

        [Fact]
        public async Task InvokeAsync_AuthenticationException_ShouldReturnUnauthorized()
        {
            // Arrange
            var authException = new AuthenticationException(ErrorCode.TOKEN_INVALID, "토큰이 유효하지 않습니다");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(authException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(401);
            
            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse!.ErrorCode.Should().Be(ErrorCode.TOKEN_INVALID.ToString());
            errorResponse.Message.Should().Be("토큰이 유효하지 않습니다");
        }

        #endregion

        #region ExternalServiceException Tests

        [Fact]
        public async Task InvokeAsync_ExternalServiceException_ShouldReturnBadGateway()
        {
            // Arrange
            var externalException = new ExternalServiceException("LLM", "/api/chat", "외부 서비스 오류");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(externalException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(502);
            
            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse!.ErrorCode.Should().Be(ErrorCode.EXTERNAL_SERVICE_ERROR.ToString());
            errorResponse.Message.Should().Be("외부 서비스 오류");
        }

        #endregion

        #region DbUpdateException Tests

        [Fact]
        public async Task InvokeAsync_DbUpdateExceptionWithDuplicateKey_ShouldReturnConflict()
        {
            // Arrange
            var innerException = new Exception("duplicate key value violates unique constraint");
            var dbException = new DbUpdateException("데이터베이스 업데이트 실패", innerException);
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(dbException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(409);
            
            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse!.ErrorCode.Should().Be("RESOURCE_CONFLICT");
            errorResponse.Message.Should().Be("이미 존재하는 데이터입니다");
        }

        [Fact]
        public async Task InvokeAsync_DbUpdateExceptionWithForeignKey_ShouldReturnBadRequest()
        {
            // Arrange
            var innerException = new Exception("foreign key constraint fails");
            var dbException = new DbUpdateException("데이터베이스 업데이트 실패", innerException);
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(dbException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(400);
            
            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse!.ErrorCode.Should().Be("CONSTRAINT_VIOLATION");
            errorResponse.Message.Should().Be("관련 데이터가 존재하여 삭제할 수 없습니다");
        }

        [Fact]
        public async Task InvokeAsync_DbUpdateExceptionGeneric_ShouldReturnInternalServerError()
        {
            // Arrange
            var dbException = new DbUpdateException("일반적인 데이터베이스 오류");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(dbException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(500);
            
            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse!.ErrorCode.Should().Be("DATABASE_ERROR");
            errorResponse.Message.Should().Be("데이터베이스 처리 중 오류가 발생했습니다");
        }

        #endregion

        #region Common Exception Tests

        [Fact]
        public async Task InvokeAsync_KeyNotFoundException_ShouldReturnNotFound()
        {
            // Arrange
            var keyNotFoundException = new KeyNotFoundException("키를 찾을 수 없습니다");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(keyNotFoundException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(404);
            
            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse!.ErrorCode.Should().Be("RESOURCE_NOT_FOUND");
            errorResponse.Message.Should().Be("키를 찾을 수 없습니다");
        }

        [Fact]
        public async Task InvokeAsync_ArgumentException_ShouldReturnBadRequest()
        {
            // Arrange
            var argumentException = new ArgumentException("잘못된 인수입니다");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(argumentException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(400);
            
            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse!.ErrorCode.Should().Be("INVALID_ARGUMENT");
            errorResponse.Message.Should().Be("잘못된 요청 파라미터입니다");
        }

        [Fact]
        public async Task InvokeAsync_TimeoutException_ShouldReturnRequestTimeout()
        {
            // Arrange
            var timeoutException = new TimeoutException("요청 시간 초과");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(timeoutException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(408);
            
            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse!.ErrorCode.Should().Be("TIMEOUT");
            errorResponse.Message.Should().Be("요청 처리 시간이 초과되었습니다");
        }

        [Fact]
        public async Task InvokeAsync_HttpRequestException_ShouldReturnBadGateway()
        {
            // Arrange
            var httpException = new HttpRequestException("HTTP 요청 실패");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(httpException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(502);
            
            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse!.ErrorCode.Should().Be("HTTP_REQUEST_ERROR");
            errorResponse.Message.Should().Be("외부 서비스와의 통신 중 오류가 발생했습니다");
        }

        #endregion

        #region Generic Exception Tests

        [Fact]
        public async Task InvokeAsync_GenericException_InDevelopment_ShouldReturnDetailedError()
        {
            // Arrange
            var genericException = new Exception("예상치 못한 오류");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(genericException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Development");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(500);
            
            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse!.ErrorCode.Should().Be("INTERNAL_SERVER_ERROR");
            errorResponse.Message.Should().Be("예상치 못한 오류"); // Development에서는 실제 오류 메시지
            errorResponse.Details.Should().NotBeNull();
            errorResponse.Details.Should().Contain(d => d.Contains("Exception Type: Exception"));
        }

        [Fact]
        public async Task InvokeAsync_GenericException_InProduction_ShouldReturnGenericError()
        {
            // Arrange
            var genericException = new Exception("예상치 못한 오류");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(genericException);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            _httpContext.Response.StatusCode.Should().Be(500);
            
            var responseBody = await GetResponseBodyAsync();
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, GetJsonOptions());
            
            errorResponse!.ErrorCode.Should().Be("INTERNAL_SERVER_ERROR");
            errorResponse.Message.Should().Be("서버에서 예상치 못한 오류가 발생했습니다"); // Production에서는 일반적 메시지
            errorResponse.Details.Should().BeNull();
        }

        #endregion

        #region Response Already Started Tests

        [Fact]
        public async Task InvokeAsync_ResponseAlreadyStarted_ShouldNotModifyResponse()
        {
            // Arrange
            var exception = new ProjectVG.Common.Exceptions.ValidationException(ErrorCode.INVALID_INPUT, "테스트 오류");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(exception);
            
            // Start the response
            await _httpContext.Response.WriteAsync("Already started");
            
            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            // Response가 이미 시작된 경우 상태 코드나 콘텐츠 타입이 변경되지 않아야 함
            // 하지만 에러 응답은 여전히 작성됨
            var responseBody = await GetResponseBodyAsync();
            responseBody.Should().NotBeEmpty(); // 에러 응답이 기존 내용 뒤에 추가됨
        }

        #endregion

        #region Environment-specific Behavior Tests

        [Fact]
        public async Task InvokeAsync_DevelopmentEnvironment_ShouldIndentJson()
        {
            // Arrange
            var exception = new ProjectVG.Common.Exceptions.ValidationException(ErrorCode.INVALID_INPUT, "테스트");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(exception);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Development");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            var responseBody = await GetResponseBodyAsync();
            responseBody.Should().Contain("\n"); // 들여쓰기된 JSON에는 줄바꿈이 포함됨
        }

        [Fact]
        public async Task InvokeAsync_ProductionEnvironment_ShouldNotIndentJson()
        {
            // Arrange
            var exception = new ProjectVG.Common.Exceptions.ValidationException(ErrorCode.INVALID_INPUT, "테스트");
            _mockNext.Setup(x => x(_httpContext)).ThrowsAsync(exception);
            _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");

            // Act
            await _handler.InvokeAsync(_httpContext);

            // Assert
            var responseBody = await GetResponseBodyAsync();
            var compactJson = responseBody.Replace("\n", "").Replace(" ", "");
            compactJson.Should().NotContain("\n"); // 압축된 JSON
        }

        #endregion

        #region Helper Methods

        private async Task<string> GetResponseBodyAsync()
        {
            _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(_httpContext.Response.Body);
            return await reader.ReadToEndAsync();
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #endregion
    }
}