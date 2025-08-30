using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Common.Constants;
using ProjectVG.Common.Exceptions;
using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Infrastructure.Persistence.Repositories.Conversation;
using Xunit;

namespace ProjectVG.Tests.Application.Services.Conversation
{
    public class ConversationServiceTests
    {
        private readonly ConversationService _conversationService;
        private readonly Mock<IConversationRepository> _mockConversationRepository;
        private readonly Mock<ILogger<ConversationService>> _mockLogger;

        public ConversationServiceTests()
        {
            _mockConversationRepository = new Mock<IConversationRepository>();
            _mockLogger = new Mock<ILogger<ConversationService>>();

            _conversationService = new ConversationService(
                _mockConversationRepository.Object,
                _mockLogger.Object
            );
        }

        #region AddMessageAsync Tests

        [Fact]
        public async Task AddMessageAsync_WithValidParameters_ShouldReturnAddedMessage()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();
            var role = ChatRole.User;
            var content = "Hello, how are you?";

            var expectedMessage = CreateTestConversationHistory(userId, characterId, role, content);

            _mockConversationRepository.Setup(x => x.AddAsync(It.IsAny<ConversationHistory>()))
                .ReturnsAsync(expectedMessage);

            // Act
            var result = await _conversationService.AddMessageAsync(userId, characterId, role, content);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(userId);
            result.CharacterId.Should().Be(characterId);
            result.Role.Should().Be(role);
            result.Content.Should().Be(content);

            _mockConversationRepository.Verify(x => x.AddAsync(It.Is<ConversationHistory>(
                msg => msg.UserId == userId &&
                       msg.CharacterId == characterId &&
                       msg.Role == role &&
                       msg.Content == content &&
                       msg.CreatedAt <= DateTime.UtcNow
            )), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddMessageAsync_WithNullOrWhitespaceContent_ShouldThrowValidationException(string content)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();
            var role = ChatRole.User;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _conversationService.AddMessageAsync(userId, characterId, role, content)
            );

            exception.ErrorCode.Should().Be(ErrorCode.MESSAGE_EMPTY);
            _mockConversationRepository.Verify(x => x.AddAsync(It.IsAny<ConversationHistory>()), Times.Never);
        }

        [Fact]
        public async Task AddMessageAsync_WithContentTooLong_ShouldThrowValidationException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();
            var role = ChatRole.User;
            var longContent = new string('x', 1001); // Exceeds 1000 character limit

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _conversationService.AddMessageAsync(userId, characterId, role, longContent)
            );

            exception.ErrorCode.Should().Be(ErrorCode.MESSAGE_TOO_LONG);
            _mockConversationRepository.Verify(x => x.AddAsync(It.IsAny<ConversationHistory>()), Times.Never);
        }

        [Theory]
        [InlineData(ChatRole.User)]
        [InlineData(ChatRole.Assistant)]
        [InlineData(ChatRole.System)]
        public async Task AddMessageAsync_WithDifferentRoles_ShouldAddMessageCorrectly(ChatRole role)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();
            var content = "Test message content";

            var expectedMessage = CreateTestConversationHistory(userId, characterId, role, content);

            _mockConversationRepository.Setup(x => x.AddAsync(It.IsAny<ConversationHistory>()))
                .ReturnsAsync(expectedMessage);

            // Act
            var result = await _conversationService.AddMessageAsync(userId, characterId, role, content);

            // Assert
            result.Should().NotBeNull();
            result.Role.Should().Be(role);
        }

        [Fact]
        public async Task AddMessageAsync_WithMaxLengthContent_ShouldAddMessageSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();
            var role = ChatRole.User;
            var maxContent = new string('x', 1000); // Exactly 1000 characters

            var expectedMessage = CreateTestConversationHistory(userId, characterId, role, maxContent);

            _mockConversationRepository.Setup(x => x.AddAsync(It.IsAny<ConversationHistory>()))
                .ReturnsAsync(expectedMessage);

            // Act
            var result = await _conversationService.AddMessageAsync(userId, characterId, role, maxContent);

            // Assert
            result.Should().NotBeNull();
            result.Content.Should().Be(maxContent);
        }

        #endregion

        #region GetConversationHistoryAsync Tests

        [Fact]
        public async Task GetConversationHistoryAsync_WithValidParameters_ShouldReturnConversationHistory()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();
            var count = 5;

            var expectedHistory = new List<ConversationHistory>
            {
                CreateTestConversationHistory(userId, characterId, ChatRole.User, "Message 1"),
                CreateTestConversationHistory(userId, characterId, ChatRole.Assistant, "Response 1"),
                CreateTestConversationHistory(userId, characterId, ChatRole.User, "Message 2")
            };

            _mockConversationRepository.Setup(x => x.GetByUserIdAsync(userId, characterId, count))
                .ReturnsAsync(expectedHistory);

            // Act
            var result = await _conversationService.GetConversationHistoryAsync(userId, characterId, count);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().BeEquivalentTo(expectedHistory);

            _mockConversationRepository.Verify(x => x.GetByUserIdAsync(userId, characterId, count), Times.Once);
        }

        [Fact]
        public async Task GetConversationHistoryAsync_WithDefaultCount_ShouldUseDefaultValue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();

            var expectedHistory = new List<ConversationHistory>();
            _mockConversationRepository.Setup(x => x.GetByUserIdAsync(userId, characterId, 10))
                .ReturnsAsync(expectedHistory);

            // Act
            var result = await _conversationService.GetConversationHistoryAsync(userId, characterId);

            // Assert
            result.Should().NotBeNull();
            _mockConversationRepository.Verify(x => x.GetByUserIdAsync(userId, characterId, 10), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(101)]
        public async Task GetConversationHistoryAsync_WithInvalidCount_ShouldThrowValidationException(int count)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _conversationService.GetConversationHistoryAsync(userId, characterId, count)
            );

            exception.ErrorCode.Should().Be(ErrorCode.VALIDATION_FAILED);
            _mockConversationRepository.Verify(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetConversationHistoryAsync_WithNoHistory_ShouldReturnEmptyCollection()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();
            var count = 10;

            _mockConversationRepository.Setup(x => x.GetByUserIdAsync(userId, characterId, count))
                .ReturnsAsync(new List<ConversationHistory>());

            // Act
            var result = await _conversationService.GetConversationHistoryAsync(userId, characterId, count);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(100)]
        public async Task GetConversationHistoryAsync_WithValidCountRange_ShouldCallRepository(int count)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();

            _mockConversationRepository.Setup(x => x.GetByUserIdAsync(userId, characterId, count))
                .ReturnsAsync(new List<ConversationHistory>());

            // Act
            await _conversationService.GetConversationHistoryAsync(userId, characterId, count);

            // Assert
            _mockConversationRepository.Verify(x => x.GetByUserIdAsync(userId, characterId, count), Times.Once);
        }

        #endregion

        #region ClearConversationAsync Tests

        [Fact]
        public async Task ClearConversationAsync_WithValidParameters_ShouldCallRepositoryClearSession()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();

            _mockConversationRepository.Setup(x => x.ClearSessionAsync(userId, characterId))
                .Returns(Task.CompletedTask);

            // Act
            await _conversationService.ClearConversationAsync(userId, characterId);

            // Assert
            _mockConversationRepository.Verify(x => x.ClearSessionAsync(userId, characterId), Times.Once);
        }

        [Fact]
        public async Task ClearConversationAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();

            _mockConversationRepository.Setup(x => x.ClearSessionAsync(userId, characterId))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => _conversationService.ClearConversationAsync(userId, characterId)
            );
        }

        #endregion

        #region GetMessageCountAsync Tests

        [Fact]
        public async Task GetMessageCountAsync_WithValidParameters_ShouldReturnMessageCount()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();
            var expectedCount = 15;

            _mockConversationRepository.Setup(x => x.GetMessageCountAsync(userId, characterId))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _conversationService.GetMessageCountAsync(userId, characterId);

            // Assert
            result.Should().Be(expectedCount);
            _mockConversationRepository.Verify(x => x.GetMessageCountAsync(userId, characterId), Times.Once);
        }

        [Fact]
        public async Task GetMessageCountAsync_WithNoMessages_ShouldReturnZero()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();

            _mockConversationRepository.Setup(x => x.GetMessageCountAsync(userId, characterId))
                .ReturnsAsync(0);

            // Act
            var result = await _conversationService.GetMessageCountAsync(userId, characterId);

            // Assert
            result.Should().Be(0);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(1000)]
        public async Task GetMessageCountAsync_WithDifferentCounts_ShouldReturnCorrectCount(int expectedCount)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();

            _mockConversationRepository.Setup(x => x.GetMessageCountAsync(userId, characterId))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _conversationService.GetMessageCountAsync(userId, characterId);

            // Assert
            result.Should().Be(expectedCount);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task AddMessageAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();
            var role = ChatRole.User;
            var content = "Test message";

            _mockConversationRepository.Setup(x => x.AddAsync(It.IsAny<ConversationHistory>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => _conversationService.AddMessageAsync(userId, characterId, role, content)
            );
        }

        [Fact]
        public async Task GetConversationHistoryAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();

            _mockConversationRepository.Setup(x => x.GetByUserIdAsync(userId, characterId, It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => _conversationService.GetConversationHistoryAsync(userId, characterId)
            );
        }

        [Fact]
        public async Task GetMessageCountAsync_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();

            _mockConversationRepository.Setup(x => x.GetMessageCountAsync(userId, characterId))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                () => _conversationService.GetMessageCountAsync(userId, characterId)
            );
        }

        [Fact]
        public async Task AddMessageAsync_WithSpecialCharacters_ShouldAddMessageSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var characterId = Guid.NewGuid();
            var role = ChatRole.User;
            var content = "특수 문자 테스트: !@#$%^&*()_+{}[]|\\:;\"'<>?,./ 한글 테스트";

            var expectedMessage = CreateTestConversationHistory(userId, characterId, role, content);

            _mockConversationRepository.Setup(x => x.AddAsync(It.IsAny<ConversationHistory>()))
                .ReturnsAsync(expectedMessage);

            // Act
            var result = await _conversationService.AddMessageAsync(userId, characterId, role, content);

            // Assert
            result.Should().NotBeNull();
            result.Content.Should().Be(content);
        }

        #endregion

        #region Helper Methods

        private static ConversationHistory CreateTestConversationHistory(
            Guid userId, 
            Guid characterId, 
            ChatRole role, 
            string content, 
            Guid? id = null)
        {
            return new ConversationHistory
            {
                Id = id ?? Guid.NewGuid(),
                UserId = userId,
                CharacterId = characterId,
                Role = role,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow,
                MetadataJson = "{}",
                IsDeleted = false
            };
        }

        #endregion
    }
}