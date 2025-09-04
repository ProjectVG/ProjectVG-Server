using Microsoft.AspNetCore.Mvc;
using ProjectVG.Api.Models.Conversation.Request;
using ProjectVG.Api.Models.Conversation.Response;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Api.Filters;
using ProjectVG.Common.Exceptions;
using ProjectVG.Common.Constants;
using System.Security.Claims;

namespace ProjectVG.Api.Controllers
{
    [ApiController]
    [Route("api/v1/conversation")]
    public class ConversationController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        private readonly ILogger<ConversationController> _logger;

        public ConversationController(IConversationService conversationService, ILogger<ConversationController> logger)
        {
            _conversationService = conversationService;
            _logger = logger;
        }

        /// <summary>
        /// 특정 캐릭터와의 대화 기록을 조회합니다
        /// </summary>
        /// <param name="characterId">캐릭터 ID</param>
        /// <param name="request">페이지네이션 요청</param>
        /// <returns>대화 기록 목록</returns>
        [HttpGet("{characterId}")]
        [JwtAuthentication]
        public async Task<IActionResult> GetConversationHistory(Guid characterId, [FromQuery] GetConversationHistoryRequest request)
        {
            try
            {
                // JWT 토큰에서 사용자 ID 추출
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    _logger.LogWarning("Authentication failed: Invalid user ID in JWT token");
                    throw new ValidationException(ErrorCode.AUTHENTICATION_FAILED);
                }

                _logger.LogDebug("Fetching conversation history: UserId={UserId}, CharacterId={CharacterId}, Page={Page}, PageSize={PageSize}", 
                    userGuid, characterId, request.Page, request.PageSize);

                // 대화 기록 조회
                var messages = await _conversationService.GetConversationHistoryAsync(userGuid, characterId, request.Page, request.PageSize);
                var totalCount = await _conversationService.GetMessageCountAsync(userGuid, characterId);
                
                _logger.LogInformation("Conversation history retrieved: UserId={UserId}, CharacterId={CharacterId}, MessageCount={MessageCount}, TotalCount={TotalCount}", 
                    userGuid, characterId, messages.Count(), totalCount);

                // 응답 매핑
                var response = new ConversationHistoryListResponse
                {
                    Messages = messages.Select(m => new ConversationHistoryResponse
                    {
                        Id = m.Id,
                        CharacterId = m.CharacterId,
                        Role = m.Role,
                        Content = m.Content,
                        Timestamp = m.Timestamp,
                        ConversationId = m.ConversationId,
                        CreatedAt = m.CreatedAt
                    }),
                    TotalCount = totalCount,
                    CurrentPage = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                    HasNextPage = request.Page < (int)Math.Ceiling((double)totalCount / request.PageSize),
                    HasPreviousPage = request.Page > 1
                };

                return Ok(response);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for conversation history request");
                return BadRequest(new { error = ex.ErrorCode, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation history for character {CharacterId}", characterId);
                return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while retrieving conversation history" });
            }
        }

        /// <summary>
        /// 특정 캐릭터와의 대화 기록을 삭제합니다
        /// </summary>
        /// <param name="characterId">캐릭터 ID</param>
        /// <returns>삭제 결과</returns>
        [HttpDelete("{characterId}")]
        [JwtAuthentication]
        public async Task<IActionResult> DeleteConversationHistory(Guid characterId)
        {
            try
            {
                // JWT 토큰에서 사용자 ID 추출
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    throw new ValidationException(ErrorCode.AUTHENTICATION_FAILED);
                }

                // 대화 기록 삭제
                await _conversationService.DeleteConversationAsync(userGuid, characterId);

                return Ok(new { message = "Conversation history deleted successfully" });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for delete conversation request");
                return BadRequest(new { error = ex.ErrorCode, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation history for character {CharacterId}", characterId);
                return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while deleting conversation history" });
            }
        }

        /// <summary>
        /// 특정 대화 세션의 메시지들을 조회합니다
        /// </summary>
        /// <param name="conversationId">대화 세션 ID</param>
        /// <returns>대화 세션 메시지 목록</returns>
        [HttpGet("session/{conversationId}")]
        [JwtAuthentication]
        public async Task<IActionResult> GetConversationBySessionId(string conversationId)
        {
            try
            {
                // JWT 토큰에서 사용자 ID 추출
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    throw new ValidationException(ErrorCode.AUTHENTICATION_FAILED);
                }

                // 대화 세션 조회 (DB 레벨에서 userId로 필터링)
                var messages = await _conversationService.GetByConversationIdAsync(conversationId, userGuid);

                // 응답 매핑
                var response = messages.Select(m => new ConversationHistoryResponse
                {
                    Id = m.Id,
                    CharacterId = m.CharacterId,
                    Role = m.Role,
                    Content = m.Content,
                    Timestamp = m.Timestamp,
                    ConversationId = m.ConversationId,
                    CreatedAt = m.CreatedAt
                });

                return Ok(response);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for conversation session request");
                return BadRequest(new { error = ex.ErrorCode, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation session {ConversationId}", conversationId);
                return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while retrieving conversation session" });
            }
        }

        /// <summary>
        /// 특정 메시지를 삭제합니다
        /// </summary>
        /// <param name="messageId">메시지 ID</param>
        /// <returns>삭제 결과</returns>
        [HttpDelete("message/{messageId}")]
        [JwtAuthentication]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            try
            {
                // JWT 토큰에서 사용자 ID 추출
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                {
                    throw new ValidationException(ErrorCode.AUTHENTICATION_FAILED);
                }

                // 메시지 삭제 (권한 확인 포함)
                await _conversationService.DeleteMessageAsync(messageId, userGuid);

                return Ok(new { message = "Message deleted successfully" });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed for delete message request");
                return BadRequest(new { error = ex.ErrorCode, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
                return StatusCode(500, new { error = "INTERNAL_ERROR", message = "An error occurred while deleting message" });
            }
        }
    }
}