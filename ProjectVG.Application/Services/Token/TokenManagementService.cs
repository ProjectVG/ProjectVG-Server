using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Services.Token;
using ProjectVG.Common.Constants;
using ProjectVG.Common.Exceptions;
using ProjectVG.Domain.Entities.Tokens;
using ProjectVG.Infrastructure.Persistence.EfCore;
using ProjectVG.Infrastructure.Persistence.Repositories.Token;
using ProjectVG.Infrastructure.Persistence.Repositories.Users;

namespace ProjectVG.Application.Services.Token
{
    /// <summary>
    /// 토큰 관리 서비스 구현
    /// 사용자 토큰 잔액 관리, 토큰 증감, 거래 기록 관리 등을 담당
    /// </summary>
    public class TokenManagementService : ITokenManagementService
    {
        private readonly ProjectVGDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly ITokenTransactionRepository _transactionRepository;
        private readonly ILogger<TokenManagementService> _logger;

        // 상수 정의
        private const decimal INITIAL_TOKEN_AMOUNT = 5000m;
        private const string INITIAL_TOKEN_SOURCE = "LOGIN_BONUS";
        private const string ROLLBACK_SOURCE = "ROLLBACK";

        public TokenManagementService(
            ProjectVGDbContext context,
            IUserRepository userRepository,
            ITokenTransactionRepository transactionRepository,
            ILogger<TokenManagementService> logger)
        {
            _context = context;
            _userRepository = userRepository;
            _transactionRepository = transactionRepository;
            _logger = logger;
        }

        public async Task<TokenBalanceInfo> GetTokenBalanceAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ValidationException(ErrorCode.USER_NOT_FOUND, $"User not found: {userId}");
            }

            return new TokenBalanceInfo
            {
                UserId = userId,
                CurrentBalance = user.TokenBalance,
                TotalEarned = user.TotalTokensEarned,
                TotalSpent = user.TotalTokensSpent,
                LastUpdated = user.UpdatedAt ?? DateTime.UtcNow,
                InitialTokensGranted = user.InitialTokensGranted
            };
        }

        public async Task<bool> HasSufficientTokensAsync(Guid userId, decimal requiredAmount)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            return user.TokenBalance >= requiredAmount;
        }

        public async Task<TokenTransactionResult> AddTokensAsync(
            Guid userId, 
            decimal amount, 
            string source, 
            string description,
            string? relatedEntityId = null,
            string? relatedEntityType = null)
        {
            if (amount <= 0)
            {
                return TokenTransactionResult.CreateFailure("Token amount must be positive");
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user == null)
                    {
                        return TokenTransactionResult.CreateFailure("User not found");
                    }

                    // 토큰 추가
                    user.TokenBalance += amount;
                    user.TotalTokensEarned += amount;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _userRepository.UpdateAsync(user);

                    // 거래 기록 생성
                    var transactionId = GenerateTransactionId();
                    var tokenTransaction = new TokenTransaction
                    {
                        UserId = userId,
                        TransactionId = transactionId,
                        Type = TokenTransactionType.Earn,
                        Amount = amount,
                        BalanceAfter = user.TokenBalance,
                        Source = source,
                        Description = description,
                        RelatedEntityId = relatedEntityId,
                        RelatedEntityType = relatedEntityType
                    };

                    await _transactionRepository.CreateAsync(tokenTransaction);
                    await transaction.CommitAsync();

                    _logger.LogInformation("Tokens added successfully: User={UserId}, Amount={Amount}, Source={Source}", 
                        userId, amount, source);

                    return TokenTransactionResult.CreateSuccess(transactionId, amount, user.TokenBalance);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to add tokens: User={UserId}, Amount={Amount}", userId, amount);
                    return TokenTransactionResult.CreateFailure("Failed to add tokens");
                }
            });
        }

        public async Task<TokenTransactionResult> DeductTokensAsync(
            Guid userId, 
            decimal amount, 
            string transactionId,
            string source, 
            string description,
            string? relatedEntityId = null,
            string? relatedEntityType = null)
        {
            if (amount <= 0)
            {
                return TokenTransactionResult.CreateFailure("Token amount must be positive");
            }

            // 중복 거래 체크
            if (await _transactionRepository.TransactionExistsAsync(transactionId))
            {
                _logger.LogWarning("Duplicate transaction attempt: {TransactionId}", transactionId);
                return TokenTransactionResult.CreateFailure("Transaction already exists");
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var dbTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user == null)
                    {
                        return TokenTransactionResult.CreateFailure("User not found");
                    }

                    // 잔액 확인
                    if (user.TokenBalance < amount)
                    {
                        _logger.LogWarning("Insufficient tokens: User={UserId}, Required={Amount}, Available={Balance}", 
                            userId, amount, user.TokenBalance);
                        return TokenTransactionResult.CreateFailure("Insufficient token balance");
                    }

                    // 토큰 차감
                    user.TokenBalance -= amount;
                    user.TotalTokensSpent += amount;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _userRepository.UpdateAsync(user);

                    // 거래 기록 생성
                    var tokenTransaction = new TokenTransaction
                    {
                        UserId = userId,
                        TransactionId = transactionId,
                        Type = TokenTransactionType.Spend,
                        Amount = -amount, // 음수로 저장하여 차감 표시
                        BalanceAfter = user.TokenBalance,
                        Source = source,
                        Description = description,
                        RelatedEntityId = relatedEntityId,
                        RelatedEntityType = relatedEntityType
                    };

                    await _transactionRepository.CreateAsync(tokenTransaction);
                    await dbTransaction.CommitAsync();

                    _logger.LogInformation("Tokens deducted successfully: User={UserId}, Amount={Amount}, Source={Source}", 
                        userId, amount, source);

                    return TokenTransactionResult.CreateSuccess(transactionId, -amount, user.TokenBalance);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to deduct tokens: User={UserId}, Amount={Amount}", userId, amount);
                    return TokenTransactionResult.CreateFailure("Failed to deduct tokens");
                }
            });
        }

        public async Task<TokenTransactionHistory> GetTokenHistoryAsync(
            Guid userId, 
            int pageNumber = 1, 
            int pageSize = 50,
            TokenTransactionType? transactionType = null)
        {
            // 페이지네이션 검증
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var (transactions, totalCount) = await _transactionRepository.GetUserTransactionsAsync(
                userId, pageNumber, pageSize, transactionType);

            var transactionInfos = transactions.Select(t => new TokenTransactionInfo
            {
                Id = t.Id,
                TransactionId = t.TransactionId,
                Type = t.Type,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                Source = t.Source,
                Description = t.Description,
                RelatedEntityId = t.RelatedEntityId,
                RelatedEntityType = t.RelatedEntityType,
                CreatedAt = t.CreatedAt
            }).ToList();

            return new TokenTransactionHistory
            {
                UserId = userId,
                Transactions = transactionInfos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<bool> GrantInitialTokensAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Cannot grant initial tokens: User not found {UserId}", userId);
                return false;
            }

            if (user.InitialTokensGranted)
            {
                _logger.LogInformation("Initial tokens already granted for user {UserId}", userId);
                return false;
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 첫 로그인 토큰 지급
                    user.TokenBalance += INITIAL_TOKEN_AMOUNT;
                    user.TotalTokensEarned += INITIAL_TOKEN_AMOUNT;
                    user.InitialTokensGranted = true;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _userRepository.UpdateAsync(user);

                    // 거래 기록 생성
                    var transactionId = GenerateTransactionId();
                    var tokenTransaction = new TokenTransaction
                    {
                        UserId = userId,
                        TransactionId = transactionId,
                        Type = TokenTransactionType.Earn,
                        Amount = INITIAL_TOKEN_AMOUNT,
                        BalanceAfter = user.TokenBalance,
                        Source = INITIAL_TOKEN_SOURCE,
                        Description = "첫 로그인 보너스 토큰",
                        RelatedEntityType = "User",
                        RelatedEntityId = userId.ToString()
                    };

                    await _transactionRepository.CreateAsync(tokenTransaction);
                    await transaction.CommitAsync();

                    _logger.LogInformation("Initial tokens granted successfully: User={UserId}, Amount={Amount}", 
                        userId, INITIAL_TOKEN_AMOUNT);

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to grant initial tokens for user {UserId}", userId);
                    return false;
                }
            });
        }

        public async Task<TokenTransactionResult> RollbackTransactionAsync(string originalTransactionId, string reason)
        {
            var originalTransaction = await _transactionRepository.GetByTransactionIdAsync(originalTransactionId);
            if (originalTransaction == null)
            {
                return TokenTransactionResult.CreateFailure("Original transaction not found");
            }

            // 이미 롤백된 거래인지 확인
            var existingRollback = await _transactionRepository.GetByRelatedEntityAsync("TokenTransaction", originalTransactionId);
            if (existingRollback.Any(t => t.Source == ROLLBACK_SOURCE))
            {
                return TokenTransactionResult.CreateFailure("Transaction already rolled back");
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var user = await _userRepository.GetByIdAsync(originalTransaction.UserId);
                    if (user == null)
                    {
                        return TokenTransactionResult.CreateFailure("User not found");
                    }

                    // 롤백 처리: 원래 거래의 반대 동작 수행
                    var rollbackAmount = -originalTransaction.Amount; // 원래 거래의 반대
                    user.TokenBalance += rollbackAmount;
                    
                    if (originalTransaction.Type == TokenTransactionType.Spend)
                    {
                        user.TotalTokensSpent -= Math.Abs(originalTransaction.Amount);
                    }
                    else
                    {
                        user.TotalTokensEarned -= originalTransaction.Amount;
                    }

                    user.UpdatedAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);

                    // 롤백 거래 기록 생성
                    var rollbackTransactionId = GenerateTransactionId();
                    var rollbackTransaction = new TokenTransaction
                    {
                        UserId = originalTransaction.UserId,
                        TransactionId = rollbackTransactionId,
                        Type = originalTransaction.Type == TokenTransactionType.Spend ? TokenTransactionType.Earn : TokenTransactionType.Spend,
                        Amount = rollbackAmount,
                        BalanceAfter = user.TokenBalance,
                        Source = ROLLBACK_SOURCE,
                        Description = $"롤백: {reason}",
                        RelatedEntityType = "TokenTransaction",
                        RelatedEntityId = originalTransactionId
                    };

                    await _transactionRepository.CreateAsync(rollbackTransaction);
                    await transaction.CommitAsync();

                    _logger.LogInformation("Transaction rolled back: Original={OriginalId}, Rollback={RollbackId}, Reason={Reason}", 
                        originalTransactionId, rollbackTransactionId, reason);

                    return TokenTransactionResult.CreateSuccess(rollbackTransactionId, rollbackAmount, user.TokenBalance);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to rollback transaction: {TransactionId}", originalTransactionId);
                    return TokenTransactionResult.CreateFailure("Failed to rollback transaction");
                }
            });
        }

        /// <summary>
        /// 고유한 거래 ID 생성
        /// </summary>
        private static string GenerateTransactionId()
        {
            return $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
        }
    }
}