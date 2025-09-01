using FluentAssertions;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Users;
using ProjectVG.Common.Exceptions;
using ProjectVG.Domain.Entities.ConversationHistorys;
using ProjectVG.Tests.Application.Integration.TestBase;
using ProjectVG.Tests.Application.TestUtilities;
using Xunit;

namespace ProjectVG.Tests.Application.Integration
{
    [Collection("ApplicationIntegration")]
    public class ConversationServiceIntegrationTests
    {
        private readonly IConversationService _conversationService;
        private readonly IUserService _userService;
        private readonly ICharacterService _characterService;
        private readonly ApplicationIntegrationTestFixture _fixture;

        public ConversationServiceIntegrationTests(ApplicationIntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _conversationService = fixture.GetService<IConversationService>();
            _userService = fixture.GetService<IUserService>();
            _characterService = fixture.GetService<ICharacterService>();
        }

        #region Add Message Integration Tests

        [Fact]
        public async Task AddMessageAsync_WithValidParameters_ShouldPersistMessage()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();
            var content = "Hello, this is a test message!";
            var role = ChatRole.User;

            // Act
            var addedMessage = await _conversationService.AddMessageAsync(userId, characterId, role, content);

            // Assert
            addedMessage.Should().NotBeNull();
            addedMessage.UserId.Should().Be(userId);
            addedMessage.CharacterId.Should().Be(characterId);
            addedMessage.Role.Should().Be(role);
            addedMessage.Content.Should().Be(content);
            addedMessage.Id.Should().NotBe(Guid.Empty);
            addedMessage.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        }

        [Theory]
        [InlineData(ChatRole.User)]
        [InlineData(ChatRole.Assistant)]
        [InlineData(ChatRole.System)]
        public async Task AddMessageAsync_WithDifferentRoles_ShouldPersistCorrectly(ChatRole role)
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();
            var content = $"Message from {role}";

            // Act
            var addedMessage = await _conversationService.AddMessageAsync(userId, characterId, role, content);

            // Assert
            addedMessage.Role.Should().Be(role);
            addedMessage.Content.Should().Be(content);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddMessageAsync_WithNullOrWhitespaceContent_ShouldThrowValidationException(string content)
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _conversationService.AddMessageAsync(userId, characterId, ChatRole.User, content));
        }

        [Fact]
        public async Task AddMessageAsync_WithContentTooLong_ShouldThrowValidationException()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();
            var longContent = new string('x', 1001); // Exceeds 1000 character limit

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _conversationService.AddMessageAsync(userId, characterId, ChatRole.User, longContent));
        }

        [Fact]
        public async Task AddMessageAsync_WithMaxLengthContent_ShouldSucceed()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();
            var maxContent = new string('x', 1000); // Exactly 1000 characters

            // Act
            var addedMessage = await _conversationService.AddMessageAsync(userId, characterId, ChatRole.User, maxContent);

            // Assert
            addedMessage.Should().NotBeNull();
            addedMessage.Content.Should().Be(maxContent);
            addedMessage.Content.Length.Should().Be(1000);
        }

        #endregion

        #region Get Conversation History Integration Tests

        [Fact]
        public async Task GetConversationHistoryAsync_WithExistingMessages_ShouldReturnInCorrectOrder()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();
            
            // Add messages with slight delays to ensure different timestamps
            var message1 = await _conversationService.AddMessageAsync(userId, characterId, ChatRole.User, "First message");
            await Task.Delay(10);
            var message2 = await _conversationService.AddMessageAsync(userId, characterId, ChatRole.Assistant, "Second message");
            await Task.Delay(10);
            var message3 = await _conversationService.AddMessageAsync(userId, characterId, ChatRole.User, "Third message");

            // Act
            var history = await _conversationService.GetConversationHistoryAsync(userId, characterId, 10);

            // Assert
            var historyList = history.ToList();
            historyList.Should().HaveCount(3);
            
            // Should be ordered by creation time (repository determines the order)
            historyList.Should().Contain(m => m.Id == message1.Id);
            historyList.Should().Contain(m => m.Id == message2.Id);
            historyList.Should().Contain(m => m.Id == message3.Id);
        }

        [Fact]
        public async Task GetConversationHistoryAsync_WithCountLimit_ShouldRespectLimit()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();
            
            // Add 5 messages
            for (int i = 1; i <= 5; i++)
            {
                await _conversationService.AddMessageAsync(userId, characterId, ChatRole.User, $"Message {i}");
                await Task.Delay(10); // Ensure different timestamps
            }

            // Act - Request only 3 messages
            var history = await _conversationService.GetConversationHistoryAsync(userId, characterId, 3);

            // Assert
            var historyList = history.ToList();
            historyList.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetConversationHistoryAsync_WithNoMessages_ShouldReturnEmptyCollection()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();

            // Act
            var history = await _conversationService.GetConversationHistoryAsync(userId, characterId);

            // Assert
            history.Should().NotBeNull();
            history.Should().BeEmpty();
        }

        [Fact]
        public async Task GetConversationHistoryAsync_WithDefaultCount_ShouldUse10AsDefault()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();
            
            // Add 15 messages
            for (int i = 1; i <= 15; i++)
            {
                await _conversationService.AddMessageAsync(userId, characterId, ChatRole.User, $"Message {i}");
            }

            // Act - Use default count
            var history = await _conversationService.GetConversationHistoryAsync(userId, characterId);

            // Assert
            var historyList = history.ToList();
            historyList.Should().HaveCount(10); // Default count should be 10
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(101)]
        public async Task GetConversationHistoryAsync_WithInvalidCount_ShouldThrowValidationException(int count)
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _conversationService.GetConversationHistoryAsync(userId, characterId, count));
        }

        [Fact]
        public async Task GetConversationHistoryAsync_WithMultipleUserCharacterPairs_ShouldReturnOnlyRelevantMessages()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var user1 = await CreateUserAsync("user1", "user1@example.com");
            var user2 = await CreateUserAsync("user2", "user2@example.com");
            var char1 = await CreateCharacterAsync("Character1");
            var char2 = await CreateCharacterAsync("Character2");

            // Add messages for different user-character combinations
            await _conversationService.AddMessageAsync(user1.Id, char1.Id, ChatRole.User, "User1-Char1 Message");
            await _conversationService.AddMessageAsync(user1.Id, char2.Id, ChatRole.User, "User1-Char2 Message");
            await _conversationService.AddMessageAsync(user2.Id, char1.Id, ChatRole.User, "User2-Char1 Message");
            await _conversationService.AddMessageAsync(user2.Id, char2.Id, ChatRole.User, "User2-Char2 Message");

            // Act
            var user1Char1History = await _conversationService.GetConversationHistoryAsync(user1.Id, char1.Id);

            // Assert
            var historyList = user1Char1History.ToList();
            historyList.Should().HaveCount(1);
            historyList[0].Content.Should().Be("User1-Char1 Message");
            historyList[0].UserId.Should().Be(user1.Id);
            historyList[0].CharacterId.Should().Be(char1.Id);
        }

        #endregion

        #region Clear Conversation Integration Tests

        [Fact]
        public async Task ClearConversationAsync_WithExistingMessages_ShouldRemoveAllMessages()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();
            
            // Add multiple messages
            await _conversationService.AddMessageAsync(userId, characterId, ChatRole.User, "Message 1");
            await _conversationService.AddMessageAsync(userId, characterId, ChatRole.Assistant, "Response 1");
            await _conversationService.AddMessageAsync(userId, characterId, ChatRole.User, "Message 2");

            // Verify messages exist
            var historyBefore = await _conversationService.GetConversationHistoryAsync(userId, characterId);
            historyBefore.Should().HaveCount(3);

            // Act
            await _conversationService.ClearConversationAsync(userId, characterId);

            // Assert
            var historyAfter = await _conversationService.GetConversationHistoryAsync(userId, characterId);
            historyAfter.Should().BeEmpty();
        }

        [Fact]
        public async Task ClearConversationAsync_WithNoMessages_ShouldNotThrow()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();

            // Act & Assert - Should not throw
            await _conversationService.ClearConversationAsync(userId, characterId);
        }

        [Fact]
        public async Task ClearConversationAsync_ShouldOnlyAffectSpecificUserCharacterPair()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var user1 = await CreateUserAsync("user1", "user1@example.com");
            var user2 = await CreateUserAsync("user2", "user2@example.com");
            var char1 = await CreateCharacterAsync("Character1");
            var char2 = await CreateCharacterAsync("Character2");

            // Add messages for different combinations
            await _conversationService.AddMessageAsync(user1.Id, char1.Id, ChatRole.User, "User1-Char1");
            await _conversationService.AddMessageAsync(user1.Id, char2.Id, ChatRole.User, "User1-Char2");
            await _conversationService.AddMessageAsync(user2.Id, char1.Id, ChatRole.User, "User2-Char1");

            // Act - Clear only user1-char1 conversation
            await _conversationService.ClearConversationAsync(user1.Id, char1.Id);

            // Assert
            var user1Char1History = await _conversationService.GetConversationHistoryAsync(user1.Id, char1.Id);
            var user1Char2History = await _conversationService.GetConversationHistoryAsync(user1.Id, char2.Id);
            var user2Char1History = await _conversationService.GetConversationHistoryAsync(user2.Id, char1.Id);

            user1Char1History.Should().BeEmpty(); // Should be cleared
            user1Char2History.Should().HaveCount(1); // Should remain
            user2Char1History.Should().HaveCount(1); // Should remain
        }

        #endregion

        #region Get Message Count Integration Tests

        [Fact]
        public async Task GetMessageCountAsync_WithMultipleMessages_ShouldReturnCorrectCount()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();
            
            // Add 5 messages
            for (int i = 1; i <= 5; i++)
            {
                await _conversationService.AddMessageAsync(userId, characterId, ChatRole.User, $"Message {i}");
            }

            // Act
            var count = await _conversationService.GetMessageCountAsync(userId, characterId);

            // Assert
            count.Should().Be(5);
        }

        [Fact]
        public async Task GetMessageCountAsync_WithNoMessages_ShouldReturnZero()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();

            // Act
            var count = await _conversationService.GetMessageCountAsync(userId, characterId);

            // Assert
            count.Should().Be(0);
        }

        [Fact]
        public async Task GetMessageCountAsync_AfterClearingConversation_ShouldReturnZero()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();
            
            // Add messages
            await _conversationService.AddMessageAsync(userId, characterId, ChatRole.User, "Message 1");
            await _conversationService.AddMessageAsync(userId, characterId, ChatRole.Assistant, "Response 1");

            var countBefore = await _conversationService.GetMessageCountAsync(userId, characterId);
            countBefore.Should().Be(2);

            // Act
            await _conversationService.ClearConversationAsync(userId, characterId);
            var countAfter = await _conversationService.GetMessageCountAsync(userId, characterId);

            // Assert
            countAfter.Should().Be(0);
        }

        #endregion

        #region Complex Integration Scenarios

        [Fact]
        public async Task CompleteConversationLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var (userId, characterId) = await CreateUserAndCharacterAsync();

            // Initial state - no messages
            var initialCount = await _conversationService.GetMessageCountAsync(userId, characterId);
            initialCount.Should().Be(0);

            var initialHistory = await _conversationService.GetConversationHistoryAsync(userId, characterId);
            initialHistory.Should().BeEmpty();

            // Add conversation messages
            var userMessage = await _conversationService.AddMessageAsync(
                userId, characterId, ChatRole.User, "Hello, how are you?");
            var assistantMessage = await _conversationService.AddMessageAsync(
                userId, characterId, ChatRole.Assistant, "I'm doing well, thank you for asking!");
            var followupMessage = await _conversationService.AddMessageAsync(
                userId, characterId, ChatRole.User, "That's great to hear!");

            // Verify messages were added
            var countAfterAdding = await _conversationService.GetMessageCountAsync(userId, characterId);
            countAfterAdding.Should().Be(3);

            var historyAfterAdding = await _conversationService.GetConversationHistoryAsync(userId, characterId);
            historyAfterAdding.Should().HaveCount(3);

            // Verify message content and order
            var messagesList = historyAfterAdding.ToList();
            messagesList.Should().Contain(m => m.Content == "Hello, how are you?" && m.Role == ChatRole.User);
            messagesList.Should().Contain(m => m.Content == "I'm doing well, thank you for asking!" && m.Role == ChatRole.Assistant);
            messagesList.Should().Contain(m => m.Content == "That's great to hear!" && m.Role == ChatRole.User);

            // Clear conversation
            await _conversationService.ClearConversationAsync(userId, characterId);

            // Verify conversation is cleared
            var countAfterClearing = await _conversationService.GetMessageCountAsync(userId, characterId);
            countAfterClearing.Should().Be(0);

            var historyAfterClearing = await _conversationService.GetConversationHistoryAsync(userId, characterId);
            historyAfterClearing.Should().BeEmpty();
        }

        [Fact]
        public async Task MultipleConversationsSimultaneously_ShouldIsolateCorrectly()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var user1 = await CreateUserAsync("user1", "user1@example.com");
            var user2 = await CreateUserAsync("user2", "user2@example.com");
            var character1 = await CreateCharacterAsync("Character1");
            var character2 = await CreateCharacterAsync("Character2");

            // Create conversations for different user-character pairs
            // User1 with Character1
            await _conversationService.AddMessageAsync(user1.Id, character1.Id, ChatRole.User, "User1 to Char1: Hello");
            await _conversationService.AddMessageAsync(user1.Id, character1.Id, ChatRole.Assistant, "Char1 to User1: Hi there");

            // User1 with Character2
            await _conversationService.AddMessageAsync(user1.Id, character2.Id, ChatRole.User, "User1 to Char2: Hey");

            // User2 with Character1
            await _conversationService.AddMessageAsync(user2.Id, character1.Id, ChatRole.User, "User2 to Char1: Good morning");
            await _conversationService.AddMessageAsync(user2.Id, character1.Id, ChatRole.Assistant, "Char1 to User2: Good morning!");
            await _conversationService.AddMessageAsync(user2.Id, character1.Id, ChatRole.User, "User2 to Char1: How's the weather?");

            // Verify counts for each conversation
            var user1Char1Count = await _conversationService.GetMessageCountAsync(user1.Id, character1.Id);
            var user1Char2Count = await _conversationService.GetMessageCountAsync(user1.Id, character2.Id);
            var user2Char1Count = await _conversationService.GetMessageCountAsync(user2.Id, character1.Id);
            var user2Char2Count = await _conversationService.GetMessageCountAsync(user2.Id, character2.Id);

            user1Char1Count.Should().Be(2);
            user1Char2Count.Should().Be(1);
            user2Char1Count.Should().Be(3);
            user2Char2Count.Should().Be(0);

            // Verify message isolation
            var user1Char1History = await _conversationService.GetConversationHistoryAsync(user1.Id, character1.Id);
            var user1Char1Messages = user1Char1History.ToList();
            
            user1Char1Messages.Should().HaveCount(2);
            user1Char1Messages.Should().OnlyContain(m => m.UserId == user1.Id && m.CharacterId == character1.Id);
            user1Char1Messages.Should().Contain(m => m.Content == "User1 to Char1: Hello");
            user1Char1Messages.Should().Contain(m => m.Content == "Char1 to User1: Hi there");
        }

        #endregion

        #region Helper Methods

        private async Task<(Guid userId, Guid characterId)> CreateUserAndCharacterAsync()
        {
            var user = await CreateUserAsync();
            var character = await CreateCharacterAsync();
            return (user.Id, character.Id);
        }

        private async Task<ProjectVG.Application.Models.User.UserDto> CreateUserAsync(
            string username = "testuser", 
            string email = "test@example.com")
        {
            var createCommand = TestDataBuilder.CreateUserCreateCommand(username, email);
            return await _userService.CreateUserAsync(createCommand);
        }

        private async Task<ProjectVG.Application.Models.Character.CharacterDto> CreateCharacterAsync(
            string name = "TestCharacter")
        {
            var createCommand = TestDataBuilder.CreateCreateCharacterWithFieldsCommand(name);
            return await _characterService.CreateCharacterWithFieldsAsync(createCommand);
        }

        #endregion
    }
}