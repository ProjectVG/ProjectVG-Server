using ProjectVG.Domain.Entities.Credits;

namespace ProjectVG.Infrastructure.Persistence.Repositories.Credit
{
    /// <summary>
    /// 토큰 거래 기록 저장소 인터페이스
    /// 토큰 거래 내역의 생성, 조회, 검증 등을 담당
    /// </summary>
    public interface ICreditTransactionRepository
    {
        /// <summary>
        /// 새로운 토큰 거래 기록 생성
        /// </summary>
        /// <param name="transaction">토큰 거래 엔티티</param>
        /// <returns>생성된 토큰 거래 기록</returns>
        Task<CreditTransaction> CreateAsync(CreditTransaction transaction);

        /// <summary>
        /// 거래 ID로 토큰 거래 기록 조회
        /// </summary>
        /// <param name="transactionId">거래 고유 ID</param>
        /// <returns>토큰 거래 기록 (없으면 null)</returns>
        Task<CreditTransaction?> GetByTransactionIdAsync(string transactionId);

        /// <summary>
        /// 사용자의 토큰 거래 내역을 페이지네이션으로 조회
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="pageNumber">페이지 번호 (1부터 시작)</param>
        /// <param name="pageSize">페이지 크기</param>
        /// <param name="transactionType">거래 유형 필터 (선택)</param>
        /// <returns>토큰 거래 내역 리스트</returns>
        Task<(List<CreditTransaction> Transactions, int TotalCount)> GetUserTransactionsAsync(
            Guid userId, 
            int pageNumber, 
            int pageSize,
            CreditTransactionType? transactionType = null);

        /// <summary>
        /// 특정 기간 내 사용자의 토큰 거래 총계 조회
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="startDate">시작 날짜</param>
        /// <param name="endDate">종료 날짜</param>
        /// <param name="transactionType">거래 유형 필터 (선택)</param>
        /// <returns>거래 총액</returns>
        Task<decimal> GetUserTransactionSumAsync(
            Guid userId, 
            DateTime startDate, 
            DateTime endDate,
            CreditTransactionType? transactionType = null);

        /// <summary>
        /// 관련 엔티티로 토큰 거래 기록들 조회
        /// </summary>
        /// <param name="relatedEntityType">관련 엔티티 타입</param>
        /// <param name="relatedEntityId">관련 엔티티 ID</param>
        /// <returns>관련 토큰 거래 기록들</returns>
        Task<List<CreditTransaction>> GetByRelatedEntityAsync(string relatedEntityType, string relatedEntityId);

        /// <summary>
        /// 특정 소스의 토큰 거래 기록들 조회
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="source">거래 소스</param>
        /// <param name="limit">조회 개수 제한 (선택)</param>
        /// <returns>소스별 토큰 거래 기록들</returns>
        Task<List<CreditTransaction>> GetBySourceAsync(Guid userId, string source, int? limit = null);

        /// <summary>
        /// 거래 ID 중복 여부 확인
        /// </summary>
        /// <param name="transactionId">확인할 거래 ID</param>
        /// <returns>중복 여부 (true: 중복됨)</returns>
        Task<bool> TransactionExistsAsync(string transactionId);

        /// <summary>
        /// 사용자별 최근 토큰 거래 조회
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="count">조회할 거래 개수</param>
        /// <returns>최근 토큰 거래 기록들</returns>
        Task<List<CreditTransaction>> GetRecentTransactionsAsync(Guid userId, int count = 10);
    }
}