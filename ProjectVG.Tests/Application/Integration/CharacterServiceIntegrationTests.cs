using FluentAssertions;
using ProjectVG.Application.Models.Character;
using ProjectVG.Application.Services.Character;
using ProjectVG.Common.Exceptions;
using ProjectVG.Tests.Application.Integration.TestBase;
using ProjectVG.Tests.Application.TestUtilities;
using Xunit;

namespace ProjectVG.Tests.Application.Integration
{
    [Collection("ApplicationIntegration")]
    public class CharacterServiceIntegrationTests
    {
        private readonly ICharacterService _characterService;
        private readonly ApplicationIntegrationTestFixture _fixture;

        public CharacterServiceIntegrationTests(ApplicationIntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _characterService = fixture.GetService<ICharacterService>();
        }

        #region Create and Retrieve Integration Tests

        [Fact]
        public async Task CreateAndGetCharacterAsync_ShouldPersistAndRetrieveCharacter()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var createCommand = TestDataBuilder.CreateCreateCharacterCommand(
                "Integration Test Character",
                "A character for integration testing",
                "Test Role");

            // Act - Create
            var createdCharacter = await _characterService.CreateCharacterAsync(createCommand);

            // Act - Retrieve
            var retrievedCharacter = await _characterService.GetCharacterByIdAsync(createdCharacter.Id);

            // Assert
            retrievedCharacter.Should().NotBeNull();
            retrievedCharacter.Id.Should().Be(createdCharacter.Id);
            retrievedCharacter.Name.Should().Be(createCommand.Name);
            retrievedCharacter.Description.Should().Be(createCommand.Description);
            retrievedCharacter.Role.Should().Be(createCommand.Role);
            retrievedCharacter.IsActive.Should().Be(createCommand.IsActive);
        }

        [Fact]
        public async Task CreateMultipleCharacters_GetAllCharactersAsync_ShouldReturnAllCharacters()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var commands = new[]
            {
                TestDataBuilder.CreateCreateCharacterCommand("Character 1", "Description 1", "Role 1"),
                TestDataBuilder.CreateCreateCharacterCommand("Character 2", "Description 2", "Role 2"),
                TestDataBuilder.CreateCreateCharacterCommand("Character 3", "Description 3", "Role 3")
            };

            // Act - Create characters
            var createdCharacters = new List<CharacterDto>();
            foreach (var command in commands)
            {
                var created = await _characterService.CreateCharacterAsync(command);
                createdCharacters.Add(created);
            }

            // Act - Get all
            var allCharacters = await _characterService.GetAllCharactersAsync();

            // Assert
            allCharacters.Should().HaveCount(3);
            allCharacters.Should().OnlyContain(c => createdCharacters.Any(cc => cc.Id == c.Id));
        }

        #endregion

        #region Update Integration Tests

        [Fact]
        public async Task CreateUpdateAndRetrieveCharacterAsync_ShouldPersistChanges()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var createCommand = TestDataBuilder.CreateCreateCharacterCommand("Original Name");
            var createdCharacter = await _characterService.CreateCharacterAsync(createCommand);

            var updateCommand = TestDataBuilder.CreateUpdateCharacterCommand(
                "Updated Name",
                "Updated Description",
                "Updated Role",
                true); // Keep IsActive = true so character can be retrieved

            // Act - Update
            var updatedCharacter = await _characterService.UpdateCharacterAsync(createdCharacter.Id, updateCommand);

            // Act - Retrieve after update
            var retrievedCharacter = await _characterService.GetCharacterByIdAsync(createdCharacter.Id);

            // Assert
            updatedCharacter.Should().NotBeNull();
            updatedCharacter.Id.Should().Be(createdCharacter.Id);
            updatedCharacter.Name.Should().Be(updateCommand.Name);
            updatedCharacter.Description.Should().Be(updateCommand.Description);
            updatedCharacter.Role.Should().Be(updateCommand.Role);
            updatedCharacter.IsActive.Should().Be(updateCommand.IsActive);

            retrievedCharacter.Should().BeEquivalentTo(updatedCharacter);
        }

        [Fact]
        public async Task UpdateNonExistentCharacter_ShouldThrowNotFoundException()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var nonExistentId = Guid.NewGuid();
            var updateCommand = TestDataBuilder.CreateUpdateCharacterCommand();

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _characterService.UpdateCharacterAsync(nonExistentId, updateCommand));
        }

        #endregion

        #region Delete Integration Tests

        [Fact]
        public async Task CreateDeleteAndTryRetrieveCharacterAsync_ShouldRemoveFromDatabase()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var createCommand = TestDataBuilder.CreateCreateCharacterCommand("To Be Deleted");
            var createdCharacter = await _characterService.CreateCharacterAsync(createCommand);

            // Verify character exists
            var existsBefore = await _characterService.CharacterExistsAsync(createdCharacter.Id);
            existsBefore.Should().BeTrue();

            // Act - Delete
            await _characterService.DeleteCharacterAsync(createdCharacter.Id);

            // Assert - Should not exist
            var existsAfter = await _characterService.CharacterExistsAsync(createdCharacter.Id);
            existsAfter.Should().BeFalse();

            // Should throw when trying to retrieve
            await Assert.ThrowsAsync<NotFoundException>(
                () => _characterService.GetCharacterByIdAsync(createdCharacter.Id));
        }

        [Fact]
        public async Task DeleteNonExistentCharacter_ShouldThrowNotFoundException()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var nonExistentId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _characterService.DeleteCharacterAsync(nonExistentId));
        }

        [Fact]
        public async Task DeleteCharacterFromMultipleCharacters_ShouldOnlyDeleteSpecificCharacter()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var character1 = await _characterService.CreateCharacterAsync(
                TestDataBuilder.CreateCreateCharacterCommand("Character 1"));
            var character2 = await _characterService.CreateCharacterAsync(
                TestDataBuilder.CreateCreateCharacterCommand("Character 2"));
            var character3 = await _characterService.CreateCharacterAsync(
                TestDataBuilder.CreateCreateCharacterCommand("Character 3"));

            // Act - Delete middle character
            await _characterService.DeleteCharacterAsync(character2.Id);

            // Assert
            var allCharacters = await _characterService.GetAllCharactersAsync();
            allCharacters.Should().HaveCount(2);
            allCharacters.Should().Contain(c => c.Id == character1.Id);
            allCharacters.Should().Contain(c => c.Id == character3.Id);
            allCharacters.Should().NotContain(c => c.Id == character2.Id);
        }

        #endregion

        #region Character Exists Integration Tests

        [Fact]
        public async Task CharacterExistsAsync_WithExistingCharacter_ShouldReturnTrue()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var createCommand = TestDataBuilder.CreateCreateCharacterCommand("Existing Character");
            var createdCharacter = await _characterService.CreateCharacterAsync(createCommand);

            // Act
            var exists = await _characterService.CharacterExistsAsync(createdCharacter.Id);

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task CharacterExistsAsync_WithNonExistentCharacter_ShouldReturnFalse()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var nonExistentId = Guid.NewGuid();

            // Act
            var exists = await _characterService.CharacterExistsAsync(nonExistentId);

            // Assert
            exists.Should().BeFalse();
        }

        #endregion

        #region Complex Scenarios Integration Tests

        [Fact]
        public async Task CompleteCharacterLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();

            // Create
            var createCommand = TestDataBuilder.CreateCreateCharacterCommand("Lifecycle Character");
            var createdCharacter = await _characterService.CreateCharacterAsync(createCommand);
            
            createdCharacter.Should().NotBeNull();
            createdCharacter.Name.Should().Be("Lifecycle Character");

            // Exists check
            var existsAfterCreate = await _characterService.CharacterExistsAsync(createdCharacter.Id);
            existsAfterCreate.Should().BeTrue();

            // Update
            var updateCommand = TestDataBuilder.CreateUpdateCharacterCommand("Updated Lifecycle Character");
            var updatedCharacter = await _characterService.UpdateCharacterAsync(createdCharacter.Id, updateCommand);
            
            updatedCharacter.Name.Should().Be("Updated Lifecycle Character");
            updatedCharacter.Id.Should().Be(createdCharacter.Id);

            // Retrieve after update
            var retrievedAfterUpdate = await _characterService.GetCharacterByIdAsync(createdCharacter.Id);
            retrievedAfterUpdate.Name.Should().Be("Updated Lifecycle Character");

            // Delete
            await _characterService.DeleteCharacterAsync(createdCharacter.Id);

            // Verify deletion
            var existsAfterDelete = await _characterService.CharacterExistsAsync(createdCharacter.Id);
            existsAfterDelete.Should().BeFalse();

            // Should throw when trying to retrieve deleted character
            await Assert.ThrowsAsync<NotFoundException>(
                () => _characterService.GetCharacterByIdAsync(createdCharacter.Id));
        }

        [Fact]
        public async Task CreateCharactersWithSameName_ShouldAllowDuplicateNames()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var sameName = "Duplicate Name Character";

            // Act - Create multiple characters with the same name
            var character1 = await _characterService.CreateCharacterAsync(
                TestDataBuilder.CreateCreateCharacterCommand(sameName, "Description 1"));
            var character2 = await _characterService.CreateCharacterAsync(
                TestDataBuilder.CreateCreateCharacterCommand(sameName, "Description 2"));

            // Assert
            character1.Should().NotBeNull();
            character2.Should().NotBeNull();
            character1.Id.Should().NotBe(character2.Id);
            character1.Name.Should().Be(sameName);
            character2.Name.Should().Be(sameName);
            character1.Description.Should().Be("Description 1");
            character2.Description.Should().Be("Description 2");

            var allCharacters = await _characterService.GetAllCharactersAsync();
            allCharacters.Should().HaveCount(2);
            allCharacters.Should().OnlyContain(c => c.Name == sameName);
        }

        #endregion

        #region Edge Cases Integration Tests

        [Fact]
        public async Task GetAllCharactersAsync_WithEmptyDatabase_ShouldReturnEmptyCollection()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();

            // Act
            var characters = await _characterService.GetAllCharactersAsync();

            // Assert
            characters.Should().NotBeNull();
            characters.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateCharacterWithSpecialCharacters_ShouldPersistCorrectly()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var specialName = "特殊文字キャラクター!@#$%^&*()_+-=[]{}|;':\",./<>?";
            var specialDescription = "Éñgłīšh àñd 中文 ànd العربية ànd עברית ànd русский";

            var createCommand = TestDataBuilder.CreateCreateCharacterCommand(
                specialName,
                specialDescription,
                "Special Role");

            // Act
            var createdCharacter = await _characterService.CreateCharacterAsync(createCommand);
            var retrievedCharacter = await _characterService.GetCharacterByIdAsync(createdCharacter.Id);

            // Assert
            retrievedCharacter.Name.Should().Be(specialName);
            retrievedCharacter.Description.Should().Be(specialDescription);
        }

        #endregion
    }
}