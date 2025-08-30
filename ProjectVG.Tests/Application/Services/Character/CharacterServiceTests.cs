using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Application.Models.Character;
using ProjectVG.Application.Services.Character;
using ProjectVG.Common.Constants;
using ProjectVG.Common.Exceptions;
using ProjectVG.Infrastructure.Persistence.Repositories.Characters;
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
                CreateTestCharacter("Character1"),
                CreateTestCharacter("Character2")
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
            var character = CreateTestCharacter("TestCharacter", characterId);

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

        #region CreateCharacterAsync Tests

        [Fact]
        public async Task CreateCharacterAsync_WithValidCommand_ShouldReturnCreatedCharacterDto()
        {
            // Arrange
            var command = new CreateCharacterCommand
            {
                Name = "NewCharacter",
                Description = "Test description",
                Role = "Assistant",
                IsActive = true
            };

            var createdCharacter = CreateTestCharacter(command.Name);
            
            _mockCharacterRepository.Setup(x => x.CreateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()))
                .ReturnsAsync(createdCharacter);

            // Act
            var result = await _characterService.CreateCharacterAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(command.Name);
            result.Description.Should().Be(command.Description);
            result.Role.Should().Be(command.Role);
            result.IsActive.Should().Be(command.IsActive);

            _mockCharacterRepository.Verify(x => x.CreateAsync(It.Is<ProjectVG.Domain.Entities.Characters.Character>(
                c => c.Name == command.Name &&
                     c.Description == command.Description &&
                     c.Role == command.Role &&
                     c.IsActive == command.IsActive
            )), Times.Once);
        }

        [Fact]
        public async Task CreateCharacterAsync_ShouldLogCharacterCreation()
        {
            // Arrange
            var command = new CreateCharacterCommand
            {
                Name = "LogTestCharacter",
                Description = "Test description",
                Role = "Assistant",
                IsActive = true
            };

            var createdCharacter = CreateTestCharacter(command.Name);
            
            _mockCharacterRepository.Setup(x => x.CreateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()))
                .ReturnsAsync(createdCharacter);

            // Act
            await _characterService.CreateCharacterAsync(command);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("캐릭터 생성 완료")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region UpdateCharacterAsync Tests

        [Fact]
        public async Task UpdateCharacterAsync_WithValidIdAndCommand_ShouldReturnUpdatedCharacterDto()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var existingCharacter = CreateTestCharacter("OldName", characterId);
            var command = new UpdateCharacterCommand
            {
                Name = "UpdatedName",
                Description = "Updated description",
                Role = "Updated role",
                IsActive = false
            };

            var updatedCharacter = CreateTestCharacter(command.Name, characterId);
            updatedCharacter.Description = command.Description;
            updatedCharacter.Role = command.Role;
            updatedCharacter.IsActive = command.IsActive;

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync(existingCharacter);
            _mockCharacterRepository.Setup(x => x.UpdateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()))
                .ReturnsAsync(updatedCharacter);

            // Act
            var result = await _characterService.UpdateCharacterAsync(characterId, command);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(characterId);
            result.Name.Should().Be(command.Name);
            result.Description.Should().Be(command.Description);
            result.Role.Should().Be(command.Role);
            result.IsActive.Should().Be(command.IsActive);

            _mockCharacterRepository.Verify(x => x.GetByIdAsync(characterId), Times.Once);
            _mockCharacterRepository.Verify(x => x.UpdateAsync(It.Is<ProjectVG.Domain.Entities.Characters.Character>(
                c => c.Id == characterId &&
                     c.Name == command.Name &&
                     c.Description == command.Description &&
                     c.Role == command.Role &&
                     c.IsActive == command.IsActive
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateCharacterAsync_WithNonExistentId_ShouldThrowNotFoundException()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var command = new UpdateCharacterCommand
            {
                Name = "UpdatedName",
                Description = "Updated description",
                Role = "Updated role",
                IsActive = false
            };

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync((ProjectVG.Domain.Entities.Characters.Character?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => _characterService.UpdateCharacterAsync(characterId, command)
            );

            exception.ErrorCode.Should().Be(ErrorCode.CHARACTER_NOT_FOUND);
            _mockCharacterRepository.Verify(x => x.GetByIdAsync(characterId), Times.Once);
            _mockCharacterRepository.Verify(x => x.UpdateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()), Times.Never);
        }

        [Fact]
        public async Task UpdateCharacterAsync_ShouldLogCharacterUpdate()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var existingCharacter = CreateTestCharacter("OldName", characterId);
            var command = new UpdateCharacterCommand
            {
                Name = "LogTestUpdate",
                Description = "Test description",
                Role = "Assistant",
                IsActive = true
            };

            var updatedCharacter = CreateTestCharacter(command.Name, characterId);

            _mockCharacterRepository.Setup(x => x.GetByIdAsync(characterId))
                .ReturnsAsync(existingCharacter);
            _mockCharacterRepository.Setup(x => x.UpdateAsync(It.IsAny<ProjectVG.Domain.Entities.Characters.Character>()))
                .ReturnsAsync(updatedCharacter);

            // Act
            await _characterService.UpdateCharacterAsync(characterId, command);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("캐릭터 수정 완료")),
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
            var character = CreateTestCharacter("TestCharacter", characterId);

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
            var character = CreateTestCharacter("DeleteTestCharacter", characterId);

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
            var character = CreateTestCharacter("ExistingCharacter", characterId);

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

        #region Helper Methods

        private static ProjectVG.Domain.Entities.Characters.Character CreateTestCharacter(string name, Guid? id = null)
        {
            return new ProjectVG.Domain.Entities.Characters.Character
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                Description = "Test description",
                Role = "Assistant",
                IsActive = true,
                Personality = "Friendly",
                SpeechStyle = "Casual",
                Summary = "Test character",
                UserAlias = "User",
                VoiceId = "test-voice"
            };
        }

        #endregion
    }
}