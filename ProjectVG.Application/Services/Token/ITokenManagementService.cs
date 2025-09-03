using ProjectVG.Domain.Entities.Tokens;

namespace ProjectVG.Application.Services.Token
{
    /// <summary>
    /// 토큰 관리 서비스 인터페이스
    /// 사용자의 토큰 잔액 관리, 토큰 증감, 거래 기록 등을 담당
    /// </summary>
    public interface ITokenManagementService
    {
        /// <summary>
        /// 사용자의 현재 토큰 잔액을 조회
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>토큰 잔액 정보</returns>
        Task<TokenBalanceInfo> GetTokenBalanceAsync(Guid userId);

        /// <summary>
        /// 사용자가 충분한 토큰을 보유하고 있는지 검증
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="requiredAmount">필요한 토큰 수</param>
        /// <returns>토큰 보유 여부</returns>
        Task<bool> HasSufficientTokensAsync(Guid userId, decimal requiredAmount);

        /// <summary>
        /// 토큰을 사용자에게 추가 (결제, 보너스 등)
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="amount">추가할 토큰 수</param>
        /// <param name="source">토큰 획득 소스 (LOGIN_BONUS, PAYMENT, etc.)</param>
        /// <param name="description">거래 설명</param>
        /// <param name="relatedEntityId">관련 엔티티 ID (선택)</param>
        /// <param name="relatedEntityType">관련 엔티티 타입 (선택)</param>
        /// <returns>거래 결과</returns>
        Task<TokenTransactionResult> AddTokensAsync(
            Guid userId, 
            decimal amount, 
            string source, 
            string description,
            string? relatedEntityId = null,
            string? relatedEntityType = null);

        /// <summary>
        /// 사용자의 토큰을 차감 (채팅, 서비스 이용 등)
        /// 토큰이 부족한 경우 예외 발생
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="amount">차감할 토큰 수</param>
        /// <param name="transactionId">고유 거래 ID (중복 방지용)</param>
        /// <param name="source">토큰 사용 소스 (CHAT_USAGE, SERVICE_FEE 등)</param>
        /// <param name="description">거래 설명</param>
        /// <param name="relatedEntityId">관련 엔티티 ID (선택)</param>
        /// <param name="relatedEntityType">관련 엔티티 타입 (선택)</param>
        /// <returns>거래 결과</returns>
        Task<TokenTransactionResult> DeductTokensAsync(
            Guid userId, 
            decimal amount, 
            string transactionId,
            string source, 
            string description,
            string? relatedEntityId = null,
            string? relatedEntityType = null);

        /// <summary>
        /// 사용자의 토큰 거래 내역을 조회
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="pageNumber">페이지 번호 (1부터 시작)</param>
        /// <param name="pageSize">페이지 크기</param>
        /// <param name="transactionType">거래 유형 필터 (선택)</param>
        /// <returns>토큰 거래 내역</returns>
        Task<TokenTransactionHistory> GetTokenHistoryAsync(
            Guid userId, 
            int pageNumber = 1, 
            int pageSize = 50,
            TokenTransactionType? transactionType = null);

        /// <summary>
        /// 첫 로그인 보너스 토큰 지급
        /// 이미 지급받은 경우 false 반환
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>지급 성공 여부</returns>
        Task<bool> GrantInitialTokensAsync(Guid userId);

        /// <summary>
        /// 토큰 거래를 롤백 (실패한 서비스에 대한 보상)
        /// </summary>
        /// <param name="originalTransactionId">원본 거래 ID</param>
        /// <param name="reason">롤백 사유</param>
        /// <returns>롤백 결과</returns>
        Task<TokenTransactionResult> RollbackTransactionAsync(string originalTransactionId, string reason);
    }

    /// <summary>
    /// 토큰 잔액 정보
    /// </summary>
    public class TokenBalanceInfo
    {
        public Guid UserId { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal TotalEarned { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool InitialTokensGranted { get; set; }
    }

    /// <summary>
    /// 토큰 거래 결과
    /// </summary>
    public class TokenTransactionResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime Timestamp { get; set; }
        public string? ErrorMessage { get; set; }
        
        public static TokenTransactionResult CreateSuccess(string transactionId, decimal amount, decimal balanceAfter)
        {
            return new TokenTransactionResult
            {
                Success = true,
                TransactionId = transactionId,
                Amount = amount,
                BalanceAfter = balanceAfter,
                Timestamp = DateTime.UtcNow
            };
        }
        
        public static TokenTransactionResult CreateFailure(string errorMessage)
        {
            return new TokenTransactionResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 토큰 거래 내역
    /// </summary>
    public class TokenTransactionHistory
    {
        public Guid UserId { get; set; }
        public List<TokenTransactionInfo> Transactions { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }

    /// <summary>
    /// 토큰 거래 정보
    /// </summary>
    public class TokenTransactionInfo
    {
        public int Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public TokenTransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}