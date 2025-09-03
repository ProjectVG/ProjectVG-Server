using ProjectVG.Domain.Entities.Credits;

namespace ProjectVG.Domain.Repositories
{
    public interface ICreditTransactionRepository
    {
        Task<CreditTransaction> CreateAsync(CreditTransaction transaction);
        Task<CreditTransaction?> GetByTransactionIdAsync(string transactionId);
        Task<(List<CreditTransaction> Transactions, int TotalCount)> GetUserTransactionsAsync(
            Guid userId, 
            int pageNumber, 
            int pageSize,
            CreditTransactionType? transactionType = null);
        Task<decimal> GetUserTransactionSumAsync(
            Guid userId, 
            DateTime startDate, 
            DateTime endDate,
            CreditTransactionType? transactionType = null);
        Task<List<CreditTransaction>> GetByRelatedEntityAsync(string relatedEntityType, string relatedEntityId);
        Task<List<CreditTransaction>> GetBySourceAsync(Guid userId, string source, int? limit = null);
        Task<bool> TransactionExistsAsync(string transactionId);
        Task<List<CreditTransaction>> GetRecentTransactionsAsync(Guid userId, int count = 10);
    }
}