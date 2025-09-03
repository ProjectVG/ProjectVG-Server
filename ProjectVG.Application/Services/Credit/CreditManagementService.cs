using Microsoft.EntityFrameworkCore;
using ProjectVG.Domain.Entities.Credits;
using ProjectVG.Infrastructure.Persistence.EfCore;
using ProjectVG.Infrastructure.Persistence.Repositories.Credit;
using ProjectVG.Infrastructure.Persistence.Repositories.Users;

namespace ProjectVG.Application.Services.Credit
{
    /// <summary>
    /// 크래딧 관리 서비스 구현
    /// 사용자 크래딧 잔액 관리, 크래딧 증감, 거래 기록 관리 등을 담당
    /// </summary>
    public class CreditManagementService : ICreditManagementService
    {
        private readonly ProjectVGDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly ICreditTransactionRepository _transactionRepository;
        private readonly ILogger<CreditManagementService> _logger;

        // 상수 정의
        private const decimal INITIAL_CREDIT_AMOUNT = 5000m;
        private const string INITIAL_CREDIT_SOURCE = "LOGIN_BONUS";
        private const string ROLLBACK_SOURCE = "ROLLBACK";

        public CreditManagementService(
            ProjectVGDbContext context,
            IUserRepository userRepository,
            ICreditTransactionRepository transactionRepository,
            ILogger<CreditManagementService> logger)
        {
            _context = context;
            _userRepository = userRepository;
            _transactionRepository = transactionRepository;
            _logger = logger;
        }

        public async Task<CreditBalanceInfo> GetCreditBalanceAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ValidationException(ErrorCode.USER_NOT_FOUND, $"User not found: {userId}");
            }

            return new CreditBalanceInfo
            {
                UserId = userId,
                CurrentBalance = user.CreditBalance,
                TotalEarned = user.TotalCreditsEarned,
                TotalSpent = user.TotalCreditsSpent,
                LastUpdated = user.UpdatedAt ?? DateTime.UtcNow,
                InitialCreditsGranted = user.InitialCreditsGranted
            };
        }

        public async Task<bool> HasSufficientCreditsAsync(Guid userId, decimal requiredAmount)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            return user.CreditBalance >= requiredAmount;
        }

        public async Task<CreditTransactionResult> AddCreditsAsync(
            Guid userId, 
            decimal amount, 
            string source, 
            string description,
            string? relatedEntityId = null,
            string? relatedEntityType = null)
        {
            if (amount <= 0)
            {
                return CreditTransactionResult.CreateFailure("Credit amount must be positive");
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
                        return CreditTransactionResult.CreateFailure("User not found");
                    }

                    // 크래딧 추가
                    user.CreditBalance += amount;
                    user.TotalCreditsEarned += amount;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _userRepository.UpdateAsync(user);

                    // 거래 기록 생성
                    var transactionId = GenerateTransactionId();
                    var creditTransaction = new CreditTransaction
                    {
                        UserId = userId,
                        TransactionId = transactionId,
                        Type = CreditTransactionType.Earn,
                        Amount = amount,
                        BalanceAfter = user.CreditBalance,
                        Source = source,
                        Description = description,
                        RelatedEntityId = relatedEntityId,
                        RelatedEntityType = relatedEntityType
                    };

                    await _transactionRepository.CreateAsync(creditTransaction);
                    await transaction.CommitAsync();

                    _logger.LogInformation("Credits added successfully: User={UserId}, Amount={Amount}, Source={Source}", 
                        userId, amount, source);

                    return CreditTransactionResult.CreateSuccess(transactionId, amount, user.CreditBalance);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to add credits: User={UserId}, Amount={Amount}", userId, amount);
                    return CreditTransactionResult.CreateFailure("Failed to add credits");
                }
            });
        }

        public async Task<CreditTransactionResult> DeductCreditsAsync(
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
                return CreditTransactionResult.CreateFailure("Credit amount must be positive");
            }

            // 중복 거래 체크
            if (await _transactionRepository.TransactionExistsAsync(transactionId))
            {
                _logger.LogWarning("Duplicate transaction attempt: {TransactionId}", transactionId);
                return CreditTransactionResult.CreateFailure("Transaction already exists");
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
                        return CreditTransactionResult.CreateFailure("User not found");
                    }

                    // 잔액 확인
                    if (user.CreditBalance < amount)
                    {
                        _logger.LogWarning("Insufficient credits: User={UserId}, Required={Amount}, Available={Balance}", 
                            userId, amount, user.CreditBalance);
                        return CreditTransactionResult.CreateFailure("Insufficient credit balance");
                    }

                    // 크래딧 차감
                    user.CreditBalance -= amount;
                    user.TotalCreditsSpent += amount;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _userRepository.UpdateAsync(user);

                    // 거래 기록 생성
                    var creditTransaction = new CreditTransaction
                    {
                        UserId = userId,
                        TransactionId = transactionId,
                        Type = CreditTransactionType.Spend,
                        Amount = -amount, // 음수로 저장하여 차감 표시
                        BalanceAfter = user.CreditBalance,
                        Source = source,
                        Description = description,
                        RelatedEntityId = relatedEntityId,
                        RelatedEntityType = relatedEntityType
                    };

                    await _transactionRepository.CreateAsync(creditTransaction);
                    await dbTransaction.CommitAsync();

                    _logger.LogInformation("Credits deducted successfully: User={UserId}, Amount={Amount}, Source={Source}", 
                        userId, amount, source);

                    return CreditTransactionResult.CreateSuccess(transactionId, -amount, user.CreditBalance);
                }
                catch (Exception ex)
                {
                    await dbTransaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to deduct credits: User={UserId}, Amount={Amount}", userId, amount);
                    return CreditTransactionResult.CreateFailure("Failed to deduct credits");
                }
            });
        }

        public async Task<CreditTransactionHistory> GetCreditHistoryAsync(
            Guid userId, 
            int pageNumber = 1, 
            int pageSize = 50,
            CreditTransactionType? transactionType = null)
        {
            // 페이지네이션 검증
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var (transactions, totalCount) = await _transactionRepository.GetUserTransactionsAsync(
                userId, pageNumber, pageSize, transactionType);

            var transactionInfos = transactions.Select(t => new CreditTransactionInfo
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

            return new CreditTransactionHistory
            {
                UserId = userId,
                Transactions = transactionInfos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<bool> GrantInitialCreditsAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Cannot grant initial credits: User not found {UserId}", userId);
                return false;
            }

            if (user.InitialCreditsGranted)
            {
                _logger.LogInformation("Initial credits already granted for user {UserId}", userId);
                return false;
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 첫 로그인 크래딧 지급
                    user.CreditBalance += INITIAL_CREDIT_AMOUNT;
                    user.TotalCreditsEarned += INITIAL_CREDIT_AMOUNT;
                    user.InitialCreditsGranted = true;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _userRepository.UpdateAsync(user);

                    // 거래 기록 생성
                    var transactionId = GenerateTransactionId();
                    var creditTransaction = new CreditTransaction
                    {
                        UserId = userId,
                        TransactionId = transactionId,
                        Type = CreditTransactionType.Earn,
                        Amount = INITIAL_CREDIT_AMOUNT,
                        BalanceAfter = user.CreditBalance,
                        Source = INITIAL_CREDIT_SOURCE,
                        Description = "첫 로그인 보너스 크래딧",
                        RelatedEntityType = "User",
                        RelatedEntityId = userId.ToString()
                    };

                    await _transactionRepository.CreateAsync(creditTransaction);
                    await transaction.CommitAsync();

                    _logger.LogInformation("Initial credits granted successfully: User={UserId}, Amount={Amount}", 
                        userId, INITIAL_CREDIT_AMOUNT);

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to grant initial credits for user {UserId}", userId);
                    return false;
                }
            });
        }

        public async Task<CreditTransactionResult> RollbackTransactionAsync(string originalTransactionId, string reason)
        {
            var originalTransaction = await _transactionRepository.GetByTransactionIdAsync(originalTransactionId);
            if (originalTransaction == null)
            {
                return CreditTransactionResult.CreateFailure("Original transaction not found");
            }

            // 이미 롤백된 거래인지 확인
            var existingRollback = await _transactionRepository.GetByRelatedEntityAsync("CreditTransaction", originalTransactionId);
            if (existingRollback.Any(t => t.Source == ROLLBACK_SOURCE))
            {
                return CreditTransactionResult.CreateFailure("Transaction already rolled back");
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
                        return CreditTransactionResult.CreateFailure("User not found");
                    }

                    // 롤백 처리: 원래 거래의 반대 동작 수행
                    var rollbackAmount = -originalTransaction.Amount; // 원래 거래의 반대
                    user.CreditBalance += rollbackAmount;
                    
                    if (originalTransaction.Type == CreditTransactionType.Spend)
                    {
                        user.TotalCreditsSpent -= Math.Abs(originalTransaction.Amount);
                    }
                    else
                    {
                        user.TotalCreditsEarned -= originalTransaction.Amount;
                    }

                    user.UpdatedAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);

                    // 롤백 거래 기록 생성
                    var rollbackTransactionId = GenerateTransactionId();
                    var rollbackTransaction = new CreditTransaction
                    {
                        UserId = originalTransaction.UserId,
                        TransactionId = rollbackTransactionId,
                        Type = originalTransaction.Type == CreditTransactionType.Spend ? CreditTransactionType.Earn : CreditTransactionType.Spend,
                        Amount = rollbackAmount,
                        BalanceAfter = user.CreditBalance,
                        Source = ROLLBACK_SOURCE,
                        Description = $"롤백: {reason}",
                        RelatedEntityType = "CreditTransaction",
                        RelatedEntityId = originalTransactionId
                    };

                    await _transactionRepository.CreateAsync(rollbackTransaction);
                    await transaction.CommitAsync();

                    _logger.LogInformation("Transaction rolled back: Original={OriginalId}, Rollback={RollbackId}, Reason={Reason}", 
                        originalTransactionId, rollbackTransactionId, reason);

                    return CreditTransactionResult.CreateSuccess(rollbackTransactionId, rollbackAmount, user.CreditBalance);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to rollback transaction: {TransactionId}", originalTransactionId);
                    return CreditTransactionResult.CreateFailure("Failed to rollback transaction");
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