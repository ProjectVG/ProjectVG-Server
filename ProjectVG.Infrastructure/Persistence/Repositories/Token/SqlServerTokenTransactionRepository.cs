using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Entities.Tokens;
using ProjectVG.Infrastructure.Persistence.EfCore;

namespace ProjectVG.Infrastructure.Persistence.Repositories.Token
{
    /// <summary>
    /// SQL Server 기반 토큰 거래 기록 저장소 구현
    /// </summary>
    public class SqlServerTokenTransactionRepository : ITokenTransactionRepository
    {
        private readonly ProjectVGDbContext _context;
        private readonly ILogger<SqlServerTokenTransactionRepository> _logger;

        public SqlServerTokenTransactionRepository(ProjectVGDbContext context, ILogger<SqlServerTokenTransactionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TokenTransaction> CreateAsync(TokenTransaction transaction)
        {
            try
            {
                _context.TokenTransactions.Add(transaction);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Token transaction created: {TransactionId} for User {UserId}, Amount: {Amount}", 
                    transaction.TransactionId, transaction.UserId, transaction.Amount);
                
                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create token transaction: {TransactionId}", transaction.TransactionId);
                throw;
            }
        }

        public async Task<TokenTransaction?> GetByTransactionIdAsync(string transactionId)
        {
            return await _context.TokenTransactions
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<(List<TokenTransaction> Transactions, int TotalCount)> GetUserTransactionsAsync(
            Guid userId, 
            int pageNumber, 
            int pageSize,
            TokenTransactionType? transactionType = null)
        {
            var query = _context.TokenTransactions
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
            TokenTransactionType? transactionType = null)
        {
            var query = _context.TokenTransactions
                .Where(t => t.UserId == userId && 
                           t.CreatedAt >= startDate && 
                           t.CreatedAt <= endDate);

            if (transactionType.HasValue)
            {
                query = query.Where(t => t.Type == transactionType.Value);
            }

            return await query.SumAsync(t => t.Amount);
        }

        public async Task<List<TokenTransaction>> GetByRelatedEntityAsync(string relatedEntityType, string relatedEntityId)
        {
            return await _context.TokenTransactions
                .Where(t => t.RelatedEntityType == relatedEntityType && 
                           t.RelatedEntityId == relatedEntityId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TokenTransaction>> GetBySourceAsync(Guid userId, string source, int? limit = null)
        {
            var query = _context.TokenTransactions
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
            return await _context.TokenTransactions
                .AnyAsync(t => t.TransactionId == transactionId);
        }

        public async Task<List<TokenTransaction>> GetRecentTransactionsAsync(Guid userId, int count = 10)
        {
            return await _context.TokenTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}