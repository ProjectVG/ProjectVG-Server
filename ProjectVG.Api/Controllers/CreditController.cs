using Microsoft.AspNetCore.Mvc;
using ProjectVG.Application.Services.Credit;
using ProjectVG.Api.Filters;
using System.Security.Claims;

namespace ProjectVG.Api.Controllers
{
    /// <summary>
    /// 크래딧 관리 API 컨트롤러
    /// 사용자의 크래딧 잔액 조회, 거래 내역 조회 등을 제공
    /// </summary>
    [ApiController]
    [Route("api/v1/credits")]
    [JwtAuthentication]
    public class CreditController : ControllerBase
    {
        private readonly ICreditManagementService _creditManagementService;
        private readonly ILogger<CreditController> _logger;

        public CreditController(ICreditManagementService creditManagementService, ILogger<CreditController> logger)
        {
            _creditManagementService = creditManagementService;
            _logger = logger;
        }

        /// <summary>
        /// 현재 사용자의 크래딧 잔액 조회
        /// </summary>
        /// <returns>크래딧 잔액 정보</returns>
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = GetCurrentUserId();
            
            try
            {
                var balance = await _creditManagementService.GetCreditBalanceAsync(userId);
                return Ok(new
                {
                    userId = balance.UserId,
                    currentBalance = balance.CurrentBalance,
                    totalEarned = balance.TotalEarned,
                    totalSpent = balance.TotalSpent,
                    lastUpdated = balance.LastUpdated,
                    initialTokensGranted = balance.InitialCreditsGranted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get credit balance for user {UserId}", userId);
                return StatusCode(500, new { error = "Failed to retrieve credit balance" });
            }
        }

        /// <summary>
        /// 크래딧 거래 내역 조회 (페이지네이션)
        /// </summary>
        /// <param name="page">페이지 번호 (1부터 시작)</param>
        /// <param name="pageSize">페이지 크기 (최대 100)</param>
        /// <param name="type">거래 유형 필터 (Earn=1, Spend=2)</param>
        /// <returns>크래딧 거래 내역</returns>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] int? type = null)
        {
            var userId = GetCurrentUserId();
            
            // 파라미터 검증
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;
            
            Domain.Entities.Credits.CreditTransactionType? transactionType = null;
            if (type.HasValue && Enum.IsDefined(typeof(Domain.Entities.Credits.CreditTransactionType), type.Value))
            {
                transactionType = (Domain.Entities.Credits.CreditTransactionType)type.Value;
            }

            try
            {
                var history = await _creditManagementService.GetCreditHistoryAsync(userId, page, pageSize, transactionType);
                
                return Ok(new
                {
                    userId = history.UserId,
                    transactions = history.Transactions.Select(t => new
                    {
                        id = t.Id,
                        transactionId = t.TransactionId,
                        type = t.Type,
                        amount = t.Amount,
                        balanceAfter = t.BalanceAfter,
                        source = t.Source,
                        description = t.Description,
                        relatedEntityId = t.RelatedEntityId,
                        relatedEntityType = t.RelatedEntityType,
                        createdAt = t.CreatedAt
                    }),
                    pagination = new
                    {
                        totalCount = history.TotalCount,
                        pageNumber = history.PageNumber,
                        pageSize = history.PageSize,
                        totalPages = history.TotalPages,
                        hasNextPage = history.HasNextPage,
                        hasPreviousPage = history.HasPreviousPage
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get credit history for user {UserId}", userId);
                return StatusCode(500, new { error = "Failed to retrieve credit history" });
            }
        }

        /// <summary>
        /// 크래딧 충분 여부 확인
        /// </summary>
        /// <param name="amount">확인할 크래딧 수량</param>
        /// <returns>크래딧 충분 여부</returns>
        [HttpGet("check/{amount}")]
        public async Task<IActionResult> CheckSufficientCredits(decimal amount)
        {
            if (amount <= 0)
            {
                return BadRequest(new { error = "Amount must be positive" });
            }

            var userId = GetCurrentUserId();
            
            try
            {
                var hasSufficient = await _creditManagementService.HasSufficientCreditsAsync(userId, amount);
                var balance = await _creditManagementService.GetCreditBalanceAsync(userId);
                
                return Ok(new
                {
                    userId = userId,
                    requiredAmount = amount,
                    currentBalance = balance.CurrentBalance,
                    hasSufficientCredits = hasSufficient,
                    shortage = hasSufficient ? 0 : amount - balance.CurrentBalance
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check credit sufficiency for user {UserId}, amount {Amount}", userId, amount);
                return StatusCode(500, new { error = "Failed to check credit sufficiency" });
            }
        }

        /// <summary>
        /// 크래딧 추가 (관리자 전용 또는 결제 시스템 연동용)
        /// 실제 운영 환경에서는 결제 검증 로직이 필요
        /// </summary>
        /// <param name="request">크래딧 추가 요청</param>
        /// <returns>크래딧 추가 결과</returns>
        [HttpPost("add")]
        public async Task<IActionResult> AddCredits([FromBody] AddCreditRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            
            try
            {
                // 실제 운영에서는 결제 검증, 권한 확인 등이 필요
                var result = await _creditManagementService.AddCreditsAsync(
                    userId,
                    request.Amount,
                    request.Source ?? "MANUAL_ADD",
                    request.Description ?? "크래딧 수동 추가",
                    request.RelatedEntityId,
                    request.RelatedEntityType
                );

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        transactionId = result.TransactionId,
                        amount = result.Amount,
                        balanceAfter = result.BalanceAfter,
                        timestamp = result.Timestamp
                    });
                }
                else
                {
                    return BadRequest(new { error = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add credits for user {UserId}", userId);
                return StatusCode(500, new { error = "Failed to add credits" });
            }
        }

        /// <summary>
        /// JWT 토큰에서 사용자 ID 추출
        /// </summary>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID in credit");
            }
            return userId;
        }
    }

    /// <summary>
    /// 크래딧 추가 요청 모델
    /// </summary>
    public class AddCreditRequest
    {
        /// <summary>
        /// 추가할 크래딧 수량 (필수)
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 크래딧 소스 (선택, 기본값: MANUAL_ADD)
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// 거래 설명 (선택)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 관련 엔티티 ID (선택)
        /// </summary>
        public string? RelatedEntityId { get; set; }

        /// <summary>
        /// 관련 엔티티 타입 (선택)
        /// </summary>
        public string? RelatedEntityType { get; set; }
    }
}