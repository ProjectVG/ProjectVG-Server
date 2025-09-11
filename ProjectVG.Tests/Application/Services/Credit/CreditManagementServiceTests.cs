using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Application.Services.Credit;
using ProjectVG.Domain.Entities.Credits;
using ProjectVG.Domain.Entities.Users;
using ProjectVG.Domain.Repositories;
using ProjectVG.Infrastructure.Persistence.EfCore;
using ProjectVG.Infrastructure.Persistence.Repositories.Credit;
using ProjectVG.Infrastructure.Persistence.Repositories.Users;
using Xunit;

namespace ProjectVG.Tests.Application.Services.Credit
{
    public class CreditManagementServiceTests : IDisposable
    {
        private readonly ProjectVGDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly ICreditTransactionRepository _transactionRepository;
        private readonly Mock<ILogger<CreditManagementService>> _mockLogger;
        private readonly CreditManagementService _service;

        public CreditManagementServiceTests()
        {
            // Create in-memory database
            var options = new DbContextOptionsBuilder<ProjectVGDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ProjectVGDbContext(options);
            _userRepository = new SqlServerUserRepository(_context, new Mock<ILogger<SqlServerUserRepository>>().Object);
            _transactionRepository = new SqlServerCreditTransactionRepository(_context, new Mock<ILogger<SqlServerCreditTransactionRepository>>().Object);
            _mockLogger = new Mock<ILogger<CreditManagementService>>();

            _service = new CreditManagementService(
                _context,
                _userRepository,
                _transactionRepository,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task GetCreditBalanceAsync_WithValidUser_ShouldReturnCreditBalance()
        {
            // Arrange
            var user = new ProjectVG.Domain.Entities.Users.User
            {
                Id = Guid.NewGuid(),
                UID = "TEST12345",
                ProviderId = "test-provider-id",
                Provider = "test",
                Email = "test@example.com",
                Username = "testuser",
                CreditBalance = 500.50m,
                TotalCreditsEarned = 1000m,
                TotalCreditsSpent = 499.50m
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetCreditBalanceAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(user.Id);
            result.CurrentBalance.Should().Be(500.50m);
            result.TotalEarned.Should().Be(1000m);
            result.TotalSpent.Should().Be(499.50m);
        }

        [Fact]
        public async Task HasSufficientCreditsAsync_WithSufficientBalance_ShouldReturnTrue()
        {
            // Arrange
            var user = new ProjectVG.Domain.Entities.Users.User
            {
                Id = Guid.NewGuid(),
                UID = "TEST12346",
                ProviderId = "test-provider-id-2",
                Provider = "test",
                Email = "test2@example.com",
                Username = "testuser2",
                CreditBalance = 100m
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.HasSufficientCreditsAsync(user.Id, 50m);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasSufficientCreditsAsync_WithInsufficientBalance_ShouldReturnFalse()
        {
            // Arrange
            var user = new ProjectVG.Domain.Entities.Users.User
            {
                Id = Guid.NewGuid(),
                UID = "TEST12347",
                ProviderId = "test-provider-id-3",
                Provider = "test",
                Email = "test3@example.com",
                Username = "testuser3",
                CreditBalance = 30m
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.HasSufficientCreditsAsync(user.Id, 50m);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetCreditHistoryAsync_WithValidUser_ShouldReturnHistory()
        {
            // Arrange
            var user = new ProjectVG.Domain.Entities.Users.User
            {
                Id = Guid.NewGuid(),
                UID = "TEST12348",
                ProviderId = "test-provider-id-4",
                Provider = "test",
                Email = "test4@example.com",
                Username = "testuser4",
                CreditBalance = 100m
            };

            var transaction = new CreditTransaction
            {
                UserId = user.Id,
                TransactionId = "TXN-TEST-001",
                Type = CreditTransactionType.Earn,
                Amount = 100m,
                BalanceAfter = 100m,
                Source = "TEST",
                Description = "Test transaction"
            };

            _context.Users.Add(user);
            _context.CreditTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetCreditHistoryAsync(user.Id, 1, 10);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(user.Id);
            result.Transactions.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.Transactions.First().Amount.Should().Be(100m);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}