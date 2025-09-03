using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Application.Models.Character;
using ProjectVG.Application.Services.Character;
using ProjectVG.Common.Constants;
using ProjectVG.Common.Exceptions;
using ProjectVG.Domain.Repositories;
using ProjectVG.Tests.Application.TestUtilities;
using ProjectVG.Domain.Entities.Characters;
using Xunit;

namespace ProjectVG.Tests.Application.Services.Character
{
    public class CharacterServiceTests
    {
        private readonly CharacterService _characterService;
        private readonly Mock<ICharacterRepository> _mockCharacterRepository;
        private readonly Mock<ILogger<ICharacterService>> _mockLogger;

        public CharacterServiceTests()
        {
            _mockCharacterRepository = new Mock<ICharacterRepository>();
            _mockLogger = new Mock<ILogger<ICharacterService>>();

            _characterService = new CharacterService(
                _mockCharacterRepository.Object,
                _mockLogger.Object
            );
        }

        #region GetAllCharactersAsync Tests

        [Fact]
        public async Task GetAllCharactersAsync_WithExistingCharacters_ShouldReturnCharacterDtos()
        {
            // Arrange
            var characters = new List<ProjectVG.Domain.Entities.Characters.Character>
            {
                TestDataBuilder.CreateCharacterEntityWithIndividualConfig("Character1"),
                TestDataBuilder.CreateCharacterEntityWithSystemPrompt("Character2")
            };

            _mockCharacterRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(characters);

            // Act
            var result = await _characterService.GetAllCharactersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Character1");
            result.Last().Name.Should().Be("Character2");

            _mockCharacterRepository.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllCharactersAsync_WithNoCharacters_ShouldReturnEmptyCollection()
        {
            // Arrange
            _mockCharacterRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<ProjectVG.Domain.Entities.Characters.Character>());

            // Act
            var result = await _characterService.GetAllCharactersAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            _mockCharacterRepository.Verify(x => x.GetAllAsync(), Times.Once);
        }

        #endregion

        #region GetCharacterByIdAsync Tests

        [Fact]
        public async Task GetCharacterByIdAsync_WithValidId_ShouldReturnCharacterDto()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var character = TestDataBuilder.CreateCharacterEntityWithIndividualConfig("TestCharacter", characterId);

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync(character);

            // Act
            var result = await _characterService.GetCharacterByIdAsync(characterId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(characterId);
            result.Name.Should().Be("TestCharacter");

            _mockCharacterRepository.Verify(x => x.GetByIdAsync(characterId), Times.Once);
        }

        [Fact]
        public async Task GetCharacterByIdAsync_WithNonExistentId_ShouldThrowNotFoundException()
        {
            // Arrange
            var characterId = Guid.NewGuid();

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync((ProjectVG.Domain.Entities.Characters.Character?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => _characterService.GetCharacterByIdAsync(characterId)
            );

            exception.ErrorCode.Should().Be(ErrorCode.CHARACTER_NOT_FOUND);
            _mockCharacterRepository.Verify(x => x.GetByIdAsync(characterId), Times.Once);
        }

        #endregion

        #region CreateCharacterWithFieldsAsync Tests

        [Fact]
        public async Task CreateCharacterWithFieldsAsync_WithValidCommand_ShouldReturnCreatedCharacterDto()
        {
            // Arrange
            var command = TestDataBuilder.CreateCreateCharacterWithFieldsCommand(
                "NewCharacter",
                "Test description",
                true,
                "test-voice",
                "Assistant",
                "Friendly and helpful"
            );

            var createdCharacter = TestDataBuilder.CreateCharacterEntityWithIndividualConfig(
                command.Name,
                null,
                command.Description,
                command.IsActive,
                command.VoiceId,
                command.IndividualConfig.Role,
                command.IndividualConfig.Personality
            );
            
            _mockCharacterRepository.Setup(x => x.CreateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()))
                .ReturnsAsync(createdCharacter);

            // Act
            var result = await _characterService.CreateCharacterWithFieldsAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(command.Name);
            result.Description.Should().Be(command.Description);
            result.IsActive.Should().Be(command.IsActive);
            result.ConfigMode.Should().Be(CharacterConfigMode.Individual);

            _mockCharacterRepository.Verify(x => x.CreateAsync(It.Is<ProjectVG.Domain.Entities.Characters.Character>(
                c => c.Name == command.Name &&
                     c.Description == command.Description &&
                     c.IsActive == command.IsActive &&
                     c.ConfigMode == CharacterConfigMode.Individual
            )), Times.Once);
        }

        [Fact]
        public async Task CreateCharacterWithFieldsAsync_ShouldLogCharacterCreation()
        {
            // Arrange
            var command = TestDataBuilder.CreateCreateCharacterWithFieldsCommand("LogTestCharacter");
            var createdCharacter = TestDataBuilder.CreateCharacterEntityWithIndividualConfig(command.Name);
            
            _mockCharacterRepository.Setup(x => x.CreateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()))
                .ReturnsAsync(createdCharacter);

            // Act
            await _characterService.CreateCharacterWithFieldsAsync(command);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("개별 설정 캐릭터 생성 완료")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region CreateCharacterWithSystemPromptAsync Tests

        [Fact]
        public async Task CreateCharacterWithSystemPromptAsync_WithValidCommand_ShouldReturnCreatedCharacterDto()
        {
            // Arrange
            var command = TestDataBuilder.CreateCreateCharacterWithSystemPromptCommand(
                "SystemPromptCharacter",
                "Test description",
                true,
                "test-voice",
                "You are a helpful assistant."
            );

            var createdCharacter = TestDataBuilder.CreateCharacterEntityWithSystemPrompt(
                command.Name,
                null,
                command.Description,
                command.IsActive,
                command.VoiceId,
                command.SystemPrompt
            );
            
            _mockCharacterRepository.Setup(x => x.CreateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()))
                .ReturnsAsync(createdCharacter);

            // Act
            var result = await _characterService.CreateCharacterWithSystemPromptAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(command.Name);
            result.Description.Should().Be(command.Description);
            result.IsActive.Should().Be(command.IsActive);
            result.ConfigMode.Should().Be(CharacterConfigMode.SystemPrompt);
            result.SystemPrompt.Should().Be(command.SystemPrompt);

            _mockCharacterRepository.Verify(x => x.CreateAsync(It.Is<ProjectVG.Domain.Entities.Characters.Character>(
                c => c.Name == command.Name &&
                     c.Description == command.Description &&
                     c.IsActive == command.IsActive &&
                     c.ConfigMode == CharacterConfigMode.SystemPrompt &&
                     c.SystemPrompt == command.SystemPrompt
            )), Times.Once);
        }

        [Fact]
        public async Task CreateCharacterWithSystemPromptAsync_ShouldLogCharacterCreation()
        {
            // Arrange
            var command = TestDataBuilder.CreateCreateCharacterWithSystemPromptCommand("SystemPromptLogTest");
            var createdCharacter = TestDataBuilder.CreateCharacterEntityWithSystemPrompt(command.Name);
            
            _mockCharacterRepository.Setup(x => x.CreateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()))
                .ReturnsAsync(createdCharacter);

            // Act
            await _characterService.CreateCharacterWithSystemPromptAsync(command);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SystemPrompt 캐릭터 생성 완료")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region UpdateCharacterToIndividualAsync Tests

        [Fact]
        public async Task UpdateCharacterToIndividualAsync_WithValidIdAndCommand_ShouldReturnUpdatedCharacterDto()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var existingCharacter = TestDataBuilder.CreateCharacterEntityWithSystemPrompt("OldName", characterId);
            var command = TestDataBuilder.CreateUpdateCharacterToIndividualCommand(
                characterId,
                "UpdatedName",
                "Updated description",
                "test-voice",
                "Updated role",
                "Updated personality"
            );

            var updatedCharacter = TestDataBuilder.CreateCharacterEntityWithIndividualConfig(
                command.Name,
                characterId,
                command.Description,
                true,
                command.VoiceId,
                command.IndividualConfig.Role,
                command.IndividualConfig.Personality
            );

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync(existingCharacter);
            _mockCharacterRepository.Setup(x => x.UpdateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()))
                .ReturnsAsync(updatedCharacter);

            // Act
            var result = await _characterService.UpdateCharacterToIndividualAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(characterId);
            result.Name.Should().Be(command.Name);
            result.Description.Should().Be(command.Description);
            result.ConfigMode.Should().Be(CharacterConfigMode.Individual);

            _mockCharacterRepository.Verify(x => x.GetByIdAsync(characterId), Times.Once);
            _mockCharacterRepository.Verify(x => x.UpdateAsync(It.Is<ProjectVG.Domain.Entities.Characters.Character>(
                c => c.Id == characterId &&
                     c.Name == command.Name &&
                     c.Description == command.Description &&
                     c.ConfigMode == CharacterConfigMode.Individual
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateCharacterToIndividualAsync_WithNonExistentId_ShouldThrowNotFoundException()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var command = TestDataBuilder.CreateUpdateCharacterToIndividualCommand(
                characterId,
                "UpdatedName",
                "Updated description"
            );

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync((ProjectVG.Domain.Entities.Characters.Character?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => _characterService.UpdateCharacterToIndividualAsync(command)
            );

            exception.ErrorCode.Should().Be(ErrorCode.CHARACTER_NOT_FOUND);
            _mockCharacterRepository.Verify(x => x.GetByIdAsync(characterId), Times.Once);
            _mockCharacterRepository.Verify(x => x.UpdateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()), Times.Never);
        }

        [Fact]
        public async Task UpdateCharacterToIndividualAsync_ShouldLogCharacterUpdate()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var existingCharacter = TestDataBuilder.CreateCharacterEntityWithSystemPrompt("OldName", characterId);
            var command = TestDataBuilder.CreateUpdateCharacterToIndividualCommand(
                characterId,
                "LogTestUpdate",
                "Test description"
            );

            var updatedCharacter = TestDataBuilder.CreateCharacterEntityWithIndividualConfig(command.Name, characterId);

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync(existingCharacter);
            _mockCharacterRepository.Setup(x => x.UpdateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()))
                .ReturnsAsync(updatedCharacter);

            // Act
            await _characterService.UpdateCharacterToIndividualAsync(command);

            // Assert - Check for the correct log message format
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("캐릭터 개별 설정 모드로 수정 완료") && v.ToString()!.Contains("LogTestUpdate")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region UpdateCharacterToSystemPromptAsync Tests

        [Fact]
        public async Task UpdateCharacterToSystemPromptAsync_WithValidIdAndCommand_ShouldReturnUpdatedCharacterDto()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var existingCharacter = TestDataBuilder.CreateCharacterEntityWithIndividualConfig("OldName", characterId);
            var command = TestDataBuilder.CreateUpdateCharacterToSystemPromptCommand(
                characterId,
                "UpdatedSystemPromptChar",
                "Updated description",
                "test-voice",
                "You are an updated helpful assistant."
            );

            var updatedCharacter = TestDataBuilder.CreateCharacterEntityWithSystemPrompt(
                command.Name,
                characterId,
                command.Description,
                true,
                command.VoiceId,
                command.SystemPrompt
            );

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync(existingCharacter);
            _mockCharacterRepository.Setup(x => x.UpdateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()))
                .ReturnsAsync(updatedCharacter);

            // Act
            var result = await _characterService.UpdateCharacterToSystemPromptAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(characterId);
            result.Name.Should().Be(command.Name);
            result.Description.Should().Be(command.Description);
            result.ConfigMode.Should().Be(CharacterConfigMode.SystemPrompt);
            result.SystemPrompt.Should().Be(command.SystemPrompt);

            _mockCharacterRepository.Verify(x => x.GetByIdAsync(characterId), Times.Once);
            _mockCharacterRepository.Verify(x => x.UpdateAsync(It.Is<ProjectVG.Domain.Entities.Characters.Character>(
                c => c.Id == characterId &&
                     c.Name == command.Name &&
                     c.Description == command.Description &&
                     c.ConfigMode == CharacterConfigMode.SystemPrompt &&
                     c.SystemPrompt == command.SystemPrompt
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateCharacterToSystemPromptAsync_WithNonExistentId_ShouldThrowNotFoundException()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var command = TestDataBuilder.CreateUpdateCharacterToSystemPromptCommand(
                characterId,
                "UpdatedName",
                "Updated description"
            );

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync((ProjectVG.Domain.Entities.Characters.Character?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => _characterService.UpdateCharacterToSystemPromptAsync(command)
            );

            exception.ErrorCode.Should().Be(ErrorCode.CHARACTER_NOT_FOUND);
            _mockCharacterRepository.Verify(x => x.GetByIdAsync(characterId), Times.Once);
            _mockCharacterRepository.Verify(x => x.UpdateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()), Times.Never);
        }

        [Fact]
        public async Task UpdateCharacterToSystemPromptAsync_ShouldLogCharacterUpdate()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var existingCharacter = TestDataBuilder.CreateCharacterEntityWithIndividualConfig("OldName", characterId);
            var command = TestDataBuilder.CreateUpdateCharacterToSystemPromptCommand(
                characterId,
                "LogTestSystemPromptUpdate",
                "Test description"
            );

            var updatedCharacter = TestDataBuilder.CreateCharacterEntityWithSystemPrompt(command.Name, characterId);

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync(existingCharacter);
            _mockCharacterRepository.Setup(x => x.UpdateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()))
                .ReturnsAsync(updatedCharacter);

            // Act
            await _characterService.UpdateCharacterToSystemPromptAsync(command);

            // Assert - Check for the correct log message format
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("캐릭터 SystemPrompt 모드로 수정 완료") && v.ToString()!.Contains("LogTestSystemPromptUpdate")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region DeleteCharacterAsync Tests

        [Fact]
        public async Task DeleteCharacterAsync_WithValidId_ShouldDeleteCharacter()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var character = TestDataBuilder.CreateCharacterEntityWithIndividualConfig("TestCharacter", characterId);

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync(character);
            _mockCharacterRepository.Setup(x => x.DeleteAsync(characterId))
                .Returns(Task.CompletedTask);

            // Act
            await _characterService.DeleteCharacterAsync(characterId);

            // Assert
            _mockCharacterRepository.Verify(x => x.GetByIdAsync(characterId), Times.Once);
            _mockCharacterRepository.Verify(x => x.DeleteAsync(characterId), Times.Once);
        }

        [Fact]
        public async Task DeleteCharacterAsync_WithNonExistentId_ShouldThrowNotFoundException()
        {
            // Arrange
            var characterId = Guid.NewGuid();

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync((ProjectVG.Domain.Entities.Characters.Character?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => _characterService.DeleteCharacterAsync(characterId)
            );

            exception.ErrorCode.Should().Be(ErrorCode.CHARACTER_NOT_FOUND);
            _mockCharacterRepository.Verify(x => x.GetByIdAsync(characterId), Times.Once);
            _mockCharacterRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteCharacterAsync_ShouldLogCharacterDeletion()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var character = TestDataBuilder.CreateCharacterEntityWithIndividualConfig("DeleteTestCharacter", characterId);

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync(character);
            _mockCharacterRepository.Setup(x => x.DeleteAsync(characterId))
                .Returns(Task.CompletedTask);

            // Act
            await _characterService.DeleteCharacterAsync(characterId);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("캐릭터 삭제 완료")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region CharacterExistsAsync Tests

        [Fact]
        public async Task CharacterExistsAsync_WithExistingId_ShouldReturnTrue()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var character = TestDataBuilder.CreateCharacterEntityWithIndividualConfig("ExistingCharacter", characterId);

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync(character);

            // Act
            var result = await _characterService.CharacterExistsAsync(characterId);

            // Assert
            result.Should().BeTrue();
            _mockCharacterRepository.Verify(x => x.GetByIdAsync(characterId), Times.Once);
        }

        [Fact]
        public async Task CharacterExistsAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var characterId = Guid.NewGuid();

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync((ProjectVG.Domain.Entities.Characters.Character?)null);

            // Act
            var result = await _characterService.CharacterExistsAsync(characterId);

            // Assert
            result.Should().BeFalse();
            _mockCharacterRepository.Verify(x => x.GetByIdAsync(characterId), Times.Once);
        }

        #endregion

        #region Character Configuration Tests

        [Fact]
        public void Character_IndividualConfigMode_ShouldReturnCorrectEffectiveSystemPrompt()
        {
            // Arrange
            var character = TestDataBuilder.CreateCharacterEntityWithIndividualConfig(
                "TestChar",
                null,
                "Test description",
                true,
                "test-voice",
                "Assistant",
                "Friendly and helpful",
                "Casual tone",
                "A test character",
                "User"
            );

            // Act
            var effectivePrompt = character.GetEffectiveSystemPrompt();

            // Assert
            effectivePrompt.Should().Contain("역할: Assistant");
            effectivePrompt.Should().Contain("성격: Friendly and helpful");
            effectivePrompt.Should().Contain("말투: Casual tone");
            effectivePrompt.Should().Contain("요약: A test character");
            effectivePrompt.Should().Contain("사용자 호칭: User");
        }

        [Fact]
        public void Character_SystemPromptMode_ShouldReturnDirectSystemPrompt()
        {
            // Arrange
            var systemPrompt = "You are a helpful assistant.";
            var character = TestDataBuilder.CreateCharacterEntityWithSystemPrompt(
                "TestChar",
                null,
                "Test description",
                true,
                "test-voice",
                systemPrompt
            );

            // Act
            var effectivePrompt = character.GetEffectiveSystemPrompt();

            // Assert
            effectivePrompt.Should().Be(systemPrompt);
        }

        [Fact]
        public void Character_ValidateConfiguration_IndividualMode_ShouldReturnTrue()
        {
            // Arrange
            var character = TestDataBuilder.CreateCharacterEntityWithIndividualConfig(
                "TestChar",
                null,
                "Test description",
                true,
                "test-voice",
                "Assistant"
            );

            // Act
            var isValid = character.ValidateConfiguration();

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Character_ValidateConfiguration_SystemPromptMode_ShouldReturnTrue()
        {
            // Arrange
            var character = TestDataBuilder.CreateCharacterEntityWithSystemPrompt(
                "TestChar",
                null,
                "Test description",
                true,
                "test-voice",
                "You are a helpful assistant."
            );

            // Act
            var isValid = character.ValidateConfiguration();

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Character_CanStartConversation_WithValidConfig_ShouldReturnTrue()
        {
            // Arrange
            var character = TestDataBuilder.CreateCharacterEntityWithIndividualConfig(
                "TestChar",
                null,
                "Test description",
                true,  // isActive
                "test-voice",
                "Assistant"
            );

            // Act
            var canStart = character.CanStartConversation();

            // Assert
            canStart.Should().BeTrue();
        }

        [Fact]
        public void Character_CanStartConversation_WithInactiveCharacter_ShouldReturnFalse()
        {
            // Arrange
            var character = TestDataBuilder.CreateCharacterEntityWithIndividualConfig(
                "TestChar",
                null,
                "Test description",
                false,  // isActive = false
                "test-voice",
                "Assistant"
            );

            // Act
            var canStart = character.CanStartConversation();

            // Assert
            canStart.Should().BeFalse();
        }

        #endregion
    }
}