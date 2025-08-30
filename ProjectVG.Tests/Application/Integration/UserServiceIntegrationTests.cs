using FluentAssertions;
using ProjectVG.Application.Models.User;
using ProjectVG.Application.Services.Users;
using ProjectVG.Common.Exceptions;
using ProjectVG.Domain.Entities.Users;
using ProjectVG.Tests.Application.Integration.TestBase;
using ProjectVG.Tests.Application.TestUtilities;
using Xunit;

namespace ProjectVG.Tests.Application.Integration
{
    [Collection("ApplicationIntegration")]
    public class UserServiceIntegrationTests
    {
        private readonly IUserService _userService;
        private readonly ApplicationIntegrationTestFixture _fixture;

        public UserServiceIntegrationTests(ApplicationIntegrationTestFixture fixture)
        {
            _fixture = fixture;
            _userService = fixture.GetService<IUserService>();
        }

        #region Create User Integration Tests

        [Fact]
        public async Task CreateUserAsync_WithValidCommand_ShouldPersistUser()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var createCommand = TestDataBuilder.CreateUserCreateCommand(
                "testuser",
                "test@example.com",
                "provider123",
                "google");

            // Act
            var createdUser = await _userService.CreateUserAsync(createCommand);

            // Assert
            createdUser.Should().NotBeNull();
            createdUser.Username.Should().Be(createCommand.Username);
            createdUser.Email.Should().Be(createCommand.Email);
            createdUser.Provider.Should().Be(createCommand.Provider);
            createdUser.ProviderId.Should().Be(createCommand.ProviderId);
            createdUser.Status.Should().Be(AccountStatus.Active);
            createdUser.UID.Should().NotBeNullOrEmpty();
            createdUser.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldGenerateUniqueUID()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var command1 = TestDataBuilder.CreateUserCreateCommand("user1", "user1@example.com");
            var command2 = TestDataBuilder.CreateUserCreateCommand("user2", "user2@example.com");

            // Act
            var user1 = await _userService.CreateUserAsync(command1);
            var user2 = await _userService.CreateUserAsync(command2);

            // Assert
            user1.UID.Should().NotBeNullOrEmpty();
            user2.UID.Should().NotBeNullOrEmpty();
            user1.UID.Should().NotBe(user2.UID);
        }

        [Fact]
        public async Task CreateUserAsync_WithDuplicateEmail_ShouldThrowValidationException()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var email = "duplicate@example.com";
            var command1 = TestDataBuilder.CreateUserCreateCommand("user1", email);
            var command2 = TestDataBuilder.CreateUserCreateCommand("user2", email);

            // Act
            await _userService.CreateUserAsync(command1);

            // Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _userService.CreateUserAsync(command2));
        }

        [Fact]
        public async Task CreateUserAsync_WithDuplicateUsername_ShouldThrowValidationException()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var username = "duplicateuser";
            var command1 = TestDataBuilder.CreateUserCreateCommand(username, "user1@example.com");
            var command2 = TestDataBuilder.CreateUserCreateCommand(username, "user2@example.com");

            // Act
            await _userService.CreateUserAsync(command1);

            // Assert
            await Assert.ThrowsAsync<ValidationException>(
                () => _userService.CreateUserAsync(command2));
        }

        #endregion

        #region Retrieve User Integration Tests

        [Fact]
        public async Task TryGetByIdAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var createCommand = TestDataBuilder.CreateUserCreateCommand();
            var createdUser = await _userService.CreateUserAsync(createCommand);

            // Act
            var retrievedUser = await _userService.TryGetByIdAsync(createdUser.Id);

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser!.Id.Should().Be(createdUser.Id);
            retrievedUser.Username.Should().Be(createdUser.Username);
            retrievedUser.Email.Should().Be(createdUser.Email);
        }

        [Fact]
        public async Task TryGetByIdAsync_WithNonExistentUser_ShouldReturnNull()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var nonExistentId = Guid.NewGuid();

            // Act
            var retrievedUser = await _userService.TryGetByIdAsync(nonExistentId);

            // Assert
            retrievedUser.Should().BeNull();
        }

        [Fact]
        public async Task TryGetByEmailAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var email = "test@example.com";
            var createCommand = TestDataBuilder.CreateUserCreateCommand(email: email);
            var createdUser = await _userService.CreateUserAsync(createCommand);

            // Act
            var retrievedUser = await _userService.TryGetByEmailAsync(email);

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser!.Email.Should().Be(email);
            retrievedUser.Id.Should().Be(createdUser.Id);
        }

        [Fact]
        public async Task TryGetByUsernameAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var username = "testuser";
            var createCommand = TestDataBuilder.CreateUserCreateCommand(username: username);
            var createdUser = await _userService.CreateUserAsync(createCommand);

            // Act
            var retrievedUser = await _userService.TryGetByUsernameAsync(username);

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser!.Username.Should().Be(username);
            retrievedUser.Id.Should().Be(createdUser.Id);
        }

        [Fact]
        public async Task TryGetByUidAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var createCommand = TestDataBuilder.CreateUserCreateCommand();
            var createdUser = await _userService.CreateUserAsync(createCommand);

            // Act
            var retrievedUser = await _userService.TryGetByUidAsync(createdUser.UID);

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser!.UID.Should().Be(createdUser.UID);
            retrievedUser.Id.Should().Be(createdUser.Id);
        }

        [Fact]
        public async Task TryGetByProviderAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var provider = "google";
            var providerId = "google123";
            var createCommand = TestDataBuilder.CreateUserCreateCommand(
                providerId: providerId, 
                provider: provider);
            var createdUser = await _userService.CreateUserAsync(createCommand);

            // Act
            var retrievedUser = await _userService.TryGetByProviderAsync(provider, providerId);

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser!.Provider.Should().Be(provider);
            retrievedUser.ProviderId.Should().Be(providerId);
            retrievedUser.Id.Should().Be(createdUser.Id);
        }

        #endregion

        #region Delete User Integration Tests

        [Fact]
        public async Task DeleteUserAsync_WithExistingUser_ShouldMarkAsDeleted()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var createCommand = TestDataBuilder.CreateUserCreateCommand();
            var createdUser = await _userService.CreateUserAsync(createCommand);

            // Verify user exists and is active
            var existsBefore = await _userService.ExistsByIdAsync(createdUser.Id);
            existsBefore.Should().BeTrue();

            // Act
            var result = await _userService.DeleteUserAsync(createdUser.Id);

            // Assert
            result.Should().BeTrue();

            // User should not be retrievable after deletion (filtered by repository)
            var userAfterDeletion = await _userService.TryGetByIdAsync(createdUser.Id);
            userAfterDeletion.Should().BeNull();
        }

        [Fact]
        public async Task DeleteUserAsync_WithNonExistentUser_ShouldThrowNotFoundException()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var nonExistentId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(
                () => _userService.DeleteUserAsync(nonExistentId));
        }

        #endregion

        #region Exists Methods Integration Tests

        [Fact]
        public async Task ExistsByEmailAsync_WithExistingUser_ShouldReturnTrue()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var email = "exists@example.com";
            var createCommand = TestDataBuilder.CreateUserCreateCommand(email: email);
            await _userService.CreateUserAsync(createCommand);

            // Act
            var exists = await _userService.ExistsByEmailAsync(email);

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByEmailAsync_WithNonExistentUser_ShouldReturnFalse()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var nonExistentEmail = "nonexistent@example.com";

            // Act
            var exists = await _userService.ExistsByEmailAsync(nonExistentEmail);

            // Assert
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByUsernameAsync_WithExistingUser_ShouldReturnTrue()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var username = "existinguser";
            var createCommand = TestDataBuilder.CreateUserCreateCommand(username: username);
            await _userService.CreateUserAsync(createCommand);

            // Act
            var exists = await _userService.ExistsByUsernameAsync(username);

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByIdAsync_WithExistingUser_ShouldReturnTrue()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var createCommand = TestDataBuilder.CreateUserCreateCommand();
            var createdUser = await _userService.CreateUserAsync(createCommand);

            // Act
            var exists = await _userService.ExistsByIdAsync(createdUser.Id);

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByUidAsync_WithExistingUser_ShouldReturnTrue()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var createCommand = TestDataBuilder.CreateUserCreateCommand();
            var createdUser = await _userService.CreateUserAsync(createCommand);

            // Act
            var exists = await _userService.ExistsByUidAsync(createdUser.UID);

            // Assert
            exists.Should().BeTrue();
        }

        #endregion

        #region Complex Scenarios Integration Tests

        [Fact]
        public async Task CompleteUserLifecycle_ShouldWorkCorrectly()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();

            // Create
            var createCommand = TestDataBuilder.CreateUserCreateCommand("lifecycle", "lifecycle@example.com");
            var createdUser = await _userService.CreateUserAsync(createCommand);
            
            createdUser.Should().NotBeNull();
            createdUser.Username.Should().Be("lifecycle");
            createdUser.Status.Should().Be(AccountStatus.Active);

            // Verify creation through various get methods
            var byId = await _userService.TryGetByIdAsync(createdUser.Id);
            var byEmail = await _userService.TryGetByEmailAsync(createdUser.Email);
            var byUsername = await _userService.TryGetByUsernameAsync(createdUser.Username);
            var byUid = await _userService.TryGetByUidAsync(createdUser.UID);

            byId.Should().NotBeNull();
            byEmail.Should().NotBeNull();
            byUsername.Should().NotBeNull();
            byUid.Should().NotBeNull();

            // All should return the same user
            byId!.Id.Should().Be(createdUser.Id);
            byEmail!.Id.Should().Be(createdUser.Id);
            byUsername!.Id.Should().Be(createdUser.Id);
            byUid!.Id.Should().Be(createdUser.Id);

            // Verify exists methods
            (await _userService.ExistsByIdAsync(createdUser.Id)).Should().BeTrue();
            (await _userService.ExistsByEmailAsync(createdUser.Email)).Should().BeTrue();
            (await _userService.ExistsByUsernameAsync(createdUser.Username)).Should().BeTrue();
            (await _userService.ExistsByUidAsync(createdUser.UID)).Should().BeTrue();

            // Delete
            var deleteResult = await _userService.DeleteUserAsync(createdUser.Id);
            deleteResult.Should().BeTrue();

            // User should not be retrievable after deletion (filtered by repository)
            var userAfterDeletion = await _userService.TryGetByIdAsync(createdUser.Id);
            userAfterDeletion.Should().BeNull();

            // Exists methods should return false for deleted users (business logic)
            (await _userService.ExistsByIdAsync(createdUser.Id)).Should().BeFalse();
        }

        [Fact]
        public async Task CreateMultipleUsers_WithDifferentProviders_ShouldAllowSameProviderId()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var sameProviderId = "same123";

            var googleCommand = TestDataBuilder.CreateUserCreateCommand(
                "googleuser", "google@example.com", sameProviderId, "google");
            var githubCommand = TestDataBuilder.CreateUserCreateCommand(
                "githubuser", "github@example.com", sameProviderId, "github");

            // Act
            var googleUser = await _userService.CreateUserAsync(googleCommand);
            var githubUser = await _userService.CreateUserAsync(githubCommand);

            // Assert
            googleUser.Should().NotBeNull();
            githubUser.Should().NotBeNull();
            googleUser.Id.Should().NotBe(githubUser.Id);
            
            googleUser.Provider.Should().Be("google");
            githubUser.Provider.Should().Be("github");
            googleUser.ProviderId.Should().Be(sameProviderId);
            githubUser.ProviderId.Should().Be(sameProviderId);

            // Should be able to retrieve by provider + providerId
            var retrievedGoogle = await _userService.TryGetByProviderAsync("google", sameProviderId);
            var retrievedGithub = await _userService.TryGetByProviderAsync("github", sameProviderId);

            retrievedGoogle.Should().NotBeNull();
            retrievedGithub.Should().NotBeNull();
            retrievedGoogle!.Id.Should().Be(googleUser.Id);
            retrievedGithub!.Id.Should().Be(githubUser.Id);
        }

        #endregion

        #region Edge Cases Integration Tests

        [Fact]
        public async Task CreateUserWithSpecialCharacters_ShouldPersistCorrectly()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();
            var specialUsername = "ユーザー名123!@#$%";
            var specialEmail = "test+special.email@example-domain.co.uk";
            
            var createCommand = TestDataBuilder.CreateUserCreateCommand(
                specialUsername, 
                specialEmail,
                "특수文字プロバイダーID",
                "specialProvider");

            // Act
            var createdUser = await _userService.CreateUserAsync(createCommand);
            var retrievedUser = await _userService.TryGetByIdAsync(createdUser.Id);

            // Assert
            retrievedUser.Should().NotBeNull();
            retrievedUser!.Username.Should().Be(specialUsername);
            retrievedUser.Email.Should().Be(specialEmail);
            retrievedUser.ProviderId.Should().Be("특수文字プロバイダーID");
            retrievedUser.Provider.Should().Be("specialProvider");
        }

        [Fact]
        public async Task TryGetMethods_WithNonExistentData_ShouldReturnNull()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();

            // Act & Assert
            (await _userService.TryGetByIdAsync(Guid.NewGuid())).Should().BeNull();
            (await _userService.TryGetByEmailAsync("nonexistent@example.com")).Should().BeNull();
            (await _userService.TryGetByUsernameAsync("nonexistentuser")).Should().BeNull();
            (await _userService.TryGetByUidAsync("NONEXISTENT123")).Should().BeNull();
            (await _userService.TryGetByProviderAsync("provider", "nonexistent")).Should().BeNull();
        }

        [Fact]
        public async Task ExistsMethods_WithNonExistentData_ShouldReturnFalse()
        {
            // Arrange
            await _fixture.ClearDatabaseAsync();

            // Act & Assert
            (await _userService.ExistsByIdAsync(Guid.NewGuid())).Should().BeFalse();
            (await _userService.ExistsByEmailAsync("nonexistent@example.com")).Should().BeFalse();
            (await _userService.ExistsByUsernameAsync("nonexistentuser")).Should().BeFalse();
            (await _userService.ExistsByUidAsync("NONEXISTENT123")).Should().BeFalse();
        }

        #endregion
    }
}