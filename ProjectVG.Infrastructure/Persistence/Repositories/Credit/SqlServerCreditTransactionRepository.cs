using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Entities.Credits;
using ProjectVG.Domain.Repositories;
using ProjectVG.Infrastructure.Persistence.EfCore;

namespace ProjectVG.Infrastructure.Persistence.Repositories.Credit
{
    /// <summary>
    /// SQL Server 기반 크래딧 거래 기록 저장소 구현
    /// </summary>
    public class SqlServerCreditTransactionRepository : ICreditTransactionRepository
    {
        private readonly ProjectVGDbContext _context;
        private readonly ILogger<SqlServerCreditTransactionRepository> _logger;

        public SqlServerCreditTransactionRepository(ProjectVGDbContext context, ILogger<SqlServerCreditTransactionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CreditTransaction> CreateAsync(CreditTransaction transaction)
        {
            try
            {
                _context.CreditTransactions.Add(transaction);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Credit transaction created: {TransactionId} for User {UserId}, Amount: {Amount}", 
                    transaction.TransactionId, transaction.UserId, transaction.Amount);
                
                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create token transaction: {TransactionId}", transaction.TransactionId);
                throw;
            }
        }

        public async Task<CreditTransaction?> GetByTransactionIdAsync(string transactionId)
        {
            return await _context.CreditTransactions
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<(List<CreditTransaction> Transactions, int TotalCount)> GetUserTransactionsAsync(
            Guid userId, 
            int pageNumber, 
            int pageSize,
            CreditTransactionType? transactionType = null)
        {
            var query = _context.CreditTransactions
                .Where(t => t.UserId == userId);

            if (transactionType.HasValue)
            {
                query = query.Where(t => t.Type == transactionType.Value);
            }

            var totalCount = await query.CountAsync();

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (transactions, totalCount);
        }

        public async Task<decimal> GetUserTransactionSumAsync(
            Guid userId, 
            DateTime startDate, 
            DateTime endDate,
            CreditTransactionType? transactionType = null)
        {
            var query = _context.CreditTransactions
                .Where(t => t.UserId == userId && 
                           t.CreatedAt >= startDate && 
                           t.CreatedAt <= endDate);

            if (transactionType.HasValue)
            {
                query = query.Where(t => t.Type == transactionType.Value);
            }

            return await query.SumAsync(t => t.Amount);
        }

        public async Task<List<CreditTransaction>> GetByRelatedEntityAsync(string relatedEntityType, string relatedEntityId)
        {
            return await _context.CreditTransactions
                .Where(t => t.RelatedEntityType == relatedEntityType && 
                           t.RelatedEntityId == relatedEntityId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<CreditTransaction>> GetBySourceAsync(Guid userId, string source, int? limit = null)
        {
            var query = _context.CreditTransactions
                .Where(t => t.UserId == userId && t.Source == source)
                .OrderByDescending(t => t.CreatedAt);

            if (limit.HasValue)
            {
                return await query.Take(limit.Value).ToListAsync();
            }

            return await query.ToListAsync();
        }

        public async Task<bool> TransactionExistsAsync(string transactionId)
        {
            return await _context.CreditTransactions
                .AnyAsync(t => t.TransactionId == transactionId);
        }

        public async Task<List<CreditTransaction>> GetRecentTransactionsAsync(Guid userId, int count = 10)
        {
            return await _context.CreditTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}