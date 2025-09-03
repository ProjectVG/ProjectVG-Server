using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.Validators;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Token;
using ProjectVG.Application.Services.Users;
using ProjectVG.Application.Models.Character;
using ProjectVG.Infrastructure.Persistence.Session;
using ProjectVG.Domain.Entities.Characters;
using ProjectVG.Common.Exceptions;
using ProjectVG.Common.Constants;
using Xunit;

namespace ProjectVG.Tests.Services.Chat.Validators
{
    public class ChatRequestValidatorTests
    {
        private readonly ChatRequestValidator _validator;
        private readonly Mock<ISessionStorage> _mockSessionStorage;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ICharacterService> _mockCharacterService;
        private readonly Mock<ITokenManagementService> _mockTokenManagementService;
        private readonly Mock<ILogger<ChatRequestValidator>> _mockLogger;

        public ChatRequestValidatorTests()
        {
            _mockSessionStorage = new Mock<ISessionStorage>();
            _mockUserService = new Mock<IUserService>();
            _mockCharacterService = new Mock<ICharacterService>();
            _mockTokenManagementService = new Mock<ITokenManagementService>();
            _mockLogger = new Mock<ILogger<ChatRequestValidator>>();

            _validator = new ChatRequestValidator(
                _mockSessionStorage.Object,
                _mockUserService.Object,
                _mockCharacterService.Object,
                _mockTokenManagementService.Object,
                _mockLogger.Object);
        }

        #region Basic Validation Tests

        [Fact]
        public async Task ValidateAsync_ValidRequest_ShouldPassWithoutException()
        {
            // Arrange
            var command = CreateValidChatCommand();
            var character = CreateValidCharacterDto(command.CharacterId);
            var tokenBalance = CreateTokenBalance(command.UserId, 1000);

            _mockCharacterService.Setup(x => x.CharacterExistsAsync(command.CharacterId))
                .ReturnsAsync(true);
            _mockCharacterService.Setup(x => x.GetCharacterByIdAsync(command.CharacterId))
                .ReturnsAsync(character);
            _mockTokenManagementService.Setup(x => x.GetTokenBalanceAsync(command.UserId))
                .ReturnsAsync(tokenBalance);

            // Act & Assert
            await _validator.ValidateAsync(command); // Should not throw
            
            _mockCharacterService.Verify(x => x.CharacterExistsAsync(command.CharacterId), Times.Once);
            _mockTokenManagementService.Verify(x => x.GetTokenBalanceAsync(command.UserId), Times.Once);
        }

        [Fact]
        public async Task ValidateAsync_CharacterNotFound_ShouldThrowValidationException()
        {
            // Arrange
            var command = CreateValidChatCommand();
            _mockCharacterService.Setup(x => x.CharacterExistsAsync(command.CharacterId))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateAsync(command));

            exception.ErrorCode.Should().Be(ErrorCode.CHARACTER_NOT_FOUND);
            _mockCharacterService.Verify(x => x.CharacterExistsAsync(command.CharacterId), Times.Once);
            _mockTokenManagementService.Verify(x => x.GetTokenBalanceAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task ValidateAsync_EmptyUserPrompt_ShouldThrowValidationException()
        {
            // Arrange
            var command = new ChatRequestCommand(
                userId: Guid.NewGuid(),
                characterId: Guid.NewGuid(),
                userPrompt: "", // Empty prompt
                requestedAt: DateTime.UtcNow,
                useTTS: false
            );

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateAsync(command));

            exception.ErrorCode.Should().Be(ErrorCode.INVALID_INPUT);
            exception.Message.Should().Contain("User prompt cannot be empty");
            
            // Should not call external services for invalid input
            _mockCharacterService.Verify(x => x.CharacterExistsAsync(It.IsAny<Guid>()), Times.Never);
            _mockTokenManagementService.Verify(x => x.GetTokenBalanceAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task ValidateAsync_WhitespaceOnlyUserPrompt_ShouldThrowValidationException()
        {
            // Arrange
            var command = new ChatRequestCommand(
                userId: Guid.NewGuid(),
                characterId: Guid.NewGuid(),
                userPrompt: "   \t\n   ", // Whitespace only
                requestedAt: DateTime.UtcNow,
                useTTS: false
            );

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateAsync(command));

            exception.ErrorCode.Should().Be(ErrorCode.INVALID_INPUT);
            exception.Message.Should().Contain("User prompt cannot be empty");
        }

        #endregion

        #region Token Balance Validation Tests

        [Fact]
        public async Task ValidateAsync_ZeroTokenBalance_ShouldThrowInsufficientTokenException()
        {
            // Arrange
            var command = CreateValidChatCommand();
            var character = CreateValidCharacterDto(command.CharacterId);
            var tokenBalance = CreateTokenBalance(command.UserId, 0); // Zero balance

            _mockCharacterService.Setup(x => x.CharacterExistsAsync(command.CharacterId))
                .ReturnsAsync(true);
            _mockCharacterService.Setup(x => x.GetCharacterByIdAsync(command.CharacterId))
                .ReturnsAsync(character);
            _mockTokenManagementService.Setup(x => x.GetTokenBalanceAsync(command.UserId))
                .ReturnsAsync(tokenBalance);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateAsync(command));

            exception.ErrorCode.Should().Be(ErrorCode.INSUFFICIENT_TOKEN_BALANCE);
            exception.Message.Should().Contain("토큰이 부족합니다");
            exception.Message.Should().Contain("현재 잔액: 0 토큰");
            exception.Message.Should().Contain("필요 토큰: 10 토큰");

            // Verify warning was logged
            VerifyWarningLogged("토큰 잔액 부족 (0 토큰)");
        }

        [Fact]
        public async Task ValidateAsync_InsufficientTokenBalance_ShouldThrowInsufficientTokenException()
        {
            // Arrange
            var command = CreateValidChatCommand();
            var character = CreateValidCharacterDto(command.CharacterId);
            var tokenBalance = CreateTokenBalance(command.UserId, 5); // Less than required 10 tokens

            _mockCharacterService.Setup(x => x.CharacterExistsAsync(command.CharacterId))
                .ReturnsAsync(true);
            _mockCharacterService.Setup(x => x.GetCharacterByIdAsync(command.CharacterId))
                .ReturnsAsync(character);
            _mockTokenManagementService.Setup(x => x.GetTokenBalanceAsync(command.UserId))
                .ReturnsAsync(tokenBalance);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateAsync(command));

            exception.ErrorCode.Should().Be(ErrorCode.INSUFFICIENT_TOKEN_BALANCE);
            exception.Message.Should().Contain("토큰이 부족합니다");
            exception.Message.Should().Contain("현재 잔액: 5 토큰");
            exception.Message.Should().Contain("필요 토큰: 10 토큰");

            // Verify warning was logged with specific details
            VerifyWarningLoggedWithParameters("토큰 부족", command.UserId.ToString(), "5", "10");
        }

        [Fact]
        public async Task ValidateAsync_ExactlyEnoughTokens_ShouldPassValidation()
        {
            // Arrange
            var command = CreateValidChatCommand();
            var character = CreateValidCharacterDto(command.CharacterId);
            var tokenBalance = CreateTokenBalance(command.UserId, 10); // Exactly required amount

            _mockCharacterService.Setup(x => x.CharacterExistsAsync(command.CharacterId))
                .ReturnsAsync(true);
            _mockCharacterService.Setup(x => x.GetCharacterByIdAsync(command.CharacterId))
                .ReturnsAsync(character);
            _mockTokenManagementService.Setup(x => x.GetTokenBalanceAsync(command.UserId))
                .ReturnsAsync(tokenBalance);

            // Act & Assert - Should not throw
            await _validator.ValidateAsync(command);

            // Verify debug log was written
            VerifyDebugLogged("채팅 요청 검증 완료");
        }

        [Fact]
        public async Task ValidateAsync_MoreThanEnoughTokens_ShouldPassValidation()
        {
            // Arrange
            var command = CreateValidChatCommand();
            var character = CreateValidCharacterDto(command.CharacterId);
            var tokenBalance = CreateTokenBalance(command.UserId, 100); // More than enough

            _mockCharacterService.Setup(x => x.CharacterExistsAsync(command.CharacterId))
                .ReturnsAsync(true);
            _mockCharacterService.Setup(x => x.GetCharacterByIdAsync(command.CharacterId))
                .ReturnsAsync(character);
            _mockTokenManagementService.Setup(x => x.GetTokenBalanceAsync(command.UserId))
                .ReturnsAsync(tokenBalance);

            // Act & Assert - Should not throw
            await _validator.ValidateAsync(command);

            // Verify debug log was written
            VerifyDebugLogged("채팅 요청 검증 완료");
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task ValidateAsync_TokenServiceThrowsException_ShouldPropagateException()
        {
            // Arrange
            var command = CreateValidChatCommand();
            var character = CreateValidCharacterDto(command.CharacterId);

            _mockCharacterService.Setup(x => x.CharacterExistsAsync(command.CharacterId))
                .ReturnsAsync(true);
            _mockCharacterService.Setup(x => x.GetCharacterByIdAsync(command.CharacterId))
                .ReturnsAsync(character);
            _mockTokenManagementService.Setup(x => x.GetTokenBalanceAsync(command.UserId))
                .ThrowsAsync(new Exception("Token service unavailable"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _validator.ValidateAsync(command));

            exception.Message.Should().Be("Token service unavailable");
        }

        [Fact]
        public async Task ValidateAsync_CharacterServiceThrowsException_ShouldPropagateException()
        {
            // Arrange
            var command = CreateValidChatCommand();

            _mockCharacterService.Setup(x => x.CharacterExistsAsync(command.CharacterId))
                .ThrowsAsync(new Exception("Character service unavailable"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _validator.ValidateAsync(command));

            exception.Message.Should().Be("Character service unavailable");
            _mockTokenManagementService.Verify(x => x.GetTokenBalanceAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task ValidateAsync_NegativeTokenBalance_ShouldThrowInsufficientTokenException()
        {
            // Arrange
            var command = CreateValidChatCommand();
            var character = CreateValidCharacterDto(command.CharacterId);
            var tokenBalance = CreateTokenBalance(command.UserId, -5); // Negative balance

            _mockCharacterService.Setup(x => x.CharacterExistsAsync(command.CharacterId))
                .ReturnsAsync(true);
            _mockCharacterService.Setup(x => x.GetCharacterByIdAsync(command.CharacterId))
                .ReturnsAsync(character);
            _mockTokenManagementService.Setup(x => x.GetTokenBalanceAsync(command.UserId))
                .ReturnsAsync(tokenBalance);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _validator.ValidateAsync(command));

            exception.ErrorCode.Should().Be(ErrorCode.INSUFFICIENT_TOKEN_BALANCE);
            exception.Message.Should().Contain("현재 잔액: -5 토큰");

            // Verify warning was logged for zero tokens (negative counts as zero)
            VerifyWarningLogged("토큰 잔액 부족 (0 토큰)");
        }

        #endregion

        #region Helper Methods

        private static ChatRequestCommand CreateValidChatCommand()
        {
            return new ChatRequestCommand(
                userId: Guid.NewGuid(),
                characterId: Guid.NewGuid(),
                userPrompt: "Hello, how are you?",
                requestedAt: DateTime.UtcNow,
                useTTS: false
            );
        }

        private static CharacterDto CreateValidCharacterDto(Guid characterId)
        {
            var character = new ProjectVG.Domain.Entities.Characters.Character
            {
                Id = characterId,
                Name = "Test Character",
                Description = "A test character for validation",
                IsActive = true,
                VoiceId = "voice_001",
                UserId = Guid.NewGuid(),
                IsPublic = true,
                ConfigMode = ProjectVG.Domain.Entities.Characters.CharacterConfigMode.Individual,
                SystemPrompt = "You are a friendly and helpful assistant."
            };
            
            return new CharacterDto(character);
        }

        private static TokenBalanceInfo CreateTokenBalance(Guid userId, decimal balance)
        {
            return new TokenBalanceInfo
            {
                UserId = userId,
                CurrentBalance = balance,
                TotalEarned = Math.Max(balance, 0),
                TotalSpent = 0,
                LastUpdated = DateTime.UtcNow,
                InitialTokensGranted = balance > 0
            };
        }

        private void VerifyWarningLogged(string expectedMessage)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private void VerifyWarningLoggedWithParameters(string expectedMessage, string userId, string currentBalance, string requiredTokens)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => 
                        v.ToString()!.Contains(expectedMessage) &&
                        v.ToString()!.Contains(userId) &&
                        v.ToString()!.Contains(currentBalance) &&
                        v.ToString()!.Contains(requiredTokens)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private void VerifyDebugLogged(string expectedMessage)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}