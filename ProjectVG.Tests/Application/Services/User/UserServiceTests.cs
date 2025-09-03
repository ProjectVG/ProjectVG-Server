using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectVG.Application.Models.User;
using ProjectVG.Application.Services.Users;
using ProjectVG.Common.Constants;
using ProjectVG.Common.Exceptions;
using ProjectVG.Domain.Entities.Users;
using ProjectVG.Domain.Repositories;
using Xunit;

namespace ProjectVG.Tests.Application.Services.User
{
    public class UserServiceTests
    {
        private readonly UserService _userService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<UserService>> _mockLogger;

        public UserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<UserService>>();

            _userService = new UserService(
                _mockUserRepository.Object,
                _mockLogger.Object
            );
        }

        #region CreateUserAsync Tests

        [Fact]
        public async Task CreateUserAsync_WithValidCommand_ShouldReturnCreatedUserDto()
        {
            // Arrange
            var command = new UserCreateCommand(
                Username: "testuser",
                Email: "test@example.com",
                ProviderId: "provider123",
                Provider: "google"
            );

            var createdUser = CreateTestUser(command.Username, command.Email, provider: command.Provider, providerId: command.ProviderId);
            
            _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
                .ReturnsAsync((ProjectVG.Domain.Entities.Users.User?)null);
            _mockUserRepository.Setup(x => x.GetByUsernameAsync(command.Username))
                .ReturnsAsync((ProjectVG.Domain.Entities.Users.User?)null);
            _mockUserRepository.Setup(x => x.GetByUIDAsync(It.IsAny<string>()))
                .ReturnsAsync((ProjectVG.Domain.Entities.Users.User?)null);
            _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<ProjectVG.Domain.Entities.Users.User>()))
                .ReturnsAsync(createdUser);

            // Act
            var result = await _userService.CreateUserAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.Username.Should().Be(command.Username);
            result.Email.Should().Be(command.Email);
            result.Provider.Should().Be(command.Provider);
            result.ProviderId.Should().Be(command.ProviderId);
            result.Status.Should().Be(AccountStatus.Active);

            _mockUserRepository.Verify(x => x.CreateAsync(It.Is<ProjectVG.Domain.Entities.Users.User>(
                u => u.Username == command.Username &&
                     u.Email == command.Email &&
                     u.Provider == command.Provider &&
                     u.ProviderId == command.ProviderId &&
                     u.Status == AccountStatus.Active
            )), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_WithExistingEmail_ShouldThrowValidationException()
        {
            // Arrange
            var command = new UserCreateCommand(
                Username: "newuser",
                Email: "existing@example.com",
                ProviderId: "provider123",
                Provider: "google"
            );

            var existingUser = CreateTestUser("existinguser", command.Email);

            _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
                .ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _userService.CreateUserAsync(command)
            );

            exception.ErrorCode.Should().Be(ErrorCode.EMAIL_ALREADY_EXISTS);
            _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<ProjectVG.Domain.Entities.Users.User>()), Times.Never);
        }

        [Fact]
        public async Task CreateUserAsync_WithExistingUsername_ShouldThrowValidationException()
        {
            // Arrange
            var command = new UserCreateCommand(
                Username: "existinguser",
                Email: "new@example.com",
                ProviderId: "provider123",
                Provider: "google"
            );

            var existingUser = CreateTestUser(command.Username, "existing@example.com");

            _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
                .ReturnsAsync((ProjectVG.Domain.Entities.Users.User?)null);
            _mockUserRepository.Setup(x => x.GetByUsernameAsync(command.Username))
                .ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => _userService.CreateUserAsync(command)
            );

            exception.ErrorCode.Should().Be(ErrorCode.USERNAME_ALREADY_EXISTS);
            _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<ProjectVG.Domain.Entities.Users.User>()), Times.Never);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldGenerateUniqueUID()
        {
            // Arrange
            var command = new UserCreateCommand(
                Username: "testuser",
                Email: "test@example.com",
                ProviderId: "provider123",
                Provider: "google"
            );

            var createdUser = CreateTestUser(command.Username, command.Email);

            _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
                .ReturnsAsync((ProjectVG.Domain.Entities.Users.User?)null);
            _mockUserRepository.Setup(x => x.GetByUsernameAsync(command.Username))
                .ReturnsAsync((ProjectVG.Domain.Entities.Users.User?)null);
            _mockUserRepository.Setup(x => x.GetByUIDAsync(It.IsAny<string>()))
                .ReturnsAsync((ProjectVG.Domain.Entities.Users.User?)null);
            _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<ProjectVG.Domain.Entities.Users.User>()))
                .ReturnsAsync(createdUser);

            // Act
            await _userService.CreateUserAsync(command);

            // Assert
            _mockUserRepository.Verify(x => x.CreateAsync(It.Is<ProjectVG.Domain.Entities.Users.User>(
                u => !string.IsNullOrEmpty(u.UID)
            )), Times.Once);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldLogUserCreation()
        {
            // Arrange
            var command = new UserCreateCommand(
                Username: "logtest",
                Email: "logtest@example.com",
                ProviderId: "provider123",
                Provider: "google"
            );

            var createdUser = CreateTestUser(command.Username, command.Email);

            _mockUserRepository.Setup(x => x.GetByEmailAsync(command.Email))
                .ReturnsAsync((ProjectVG.Domain.Entities.Users.User?)null);
            _mockUserRepository.Setup(x => x.GetByUsernameAsync(command.Username))
                .ReturnsAsync((ProjectVG.Domain.Entities.Users.User?)null);
            _mockUserRepository.Setup(x => x.GetByUIDAsync(It.IsAny<string>()))
                .ReturnsAsync((ProjectVG.Domain.Entities.Users.User?)null);
            _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<ProjectVG.Domain.Entities.Users.User>()))
                .ReturnsAsync(createdUser);

            // Act
            await _userService.CreateUserAsync(command);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("사용자 생성 완료")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region DeleteUserAsync Tests

        [Fact]
        public async Task DeleteUserAsync_WithValidId_ShouldMarkUserAsDeletedAndReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser("testuser", "test@example.com", userId);
            user.Status = AccountStatus.Active;

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<ProjectVG.Domain.Entities.Users.User>()))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.DeleteUserAsync(userId);

            // Assert
            result.Should().BeTrue();
            user.Status.Should().Be(AccountStatus.Deleted);

            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<ProjectVG.Domain.Entities.Users.User>(
                u => u.Id == userId && u.Status == AccountStatus.Deleted
            )), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_WithNonExistentId_ShouldThrowNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((ProjectVG.Domain.Entities.Users.User?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NotFoundException>(
                () => _userService.DeleteUserAsync(userId)
            );

            exception.ErrorCode.Should().Be(ErrorCode.USER_NOT_FOUND);
            _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<ProjectVG.Domain.Entities.Users.User>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUserAsync_ShouldLogUserDeletion()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser("deletetest", "delete@example.com", userId);

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<ProjectVG.Domain.Entities.Users.User>()))
                .ReturnsAsync(user);

            // Act
            await _userService.DeleteUserAsync(userId);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("사용자 삭제 완료")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region TryGet Methods Tests

        [Fact]
        public async Task TryGetByIdAsync_WithExistingId_ShouldReturnUserDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser("testuser", "test@example.com", userId);

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.TryGetByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(userId);
            result.Username.Should().Be(user.Username);
            result.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task TryGetByIdAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((ProjectVG.Domain.Entities.Users.User?)null);

            // Act
            var result = await _userService.TryGetByIdAsync(userId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task TryGetByUidAsync_WithExistingUid_ShouldReturnUserDto()
        {
            // Arrange
            var uid = "TEST123";
            var user = CreateTestUser("testuser", "test@example.com");
            user.UID = uid;

            _mockUserRepository.Setup(x => x.GetByUIDAsync(uid))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.TryGetByUidAsync(uid);

            // Assert
            result.Should().NotBeNull();
            result!.UID.Should().Be(uid);
        }

        [Fact]
        public async Task TryGetByUsernameAsync_WithExistingUsername_ShouldReturnUserDto()
        {
            // Arrange
            var username = "testuser";
            var user = CreateTestUser(username, "test@example.com");

            _mockUserRepository.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.TryGetByUsernameAsync(username);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be(username);
        }

        [Fact]
        public async Task TryGetByEmailAsync_WithExistingEmail_ShouldReturnUserDto()
        {
            // Arrange
            var email = "test@example.com";
            var user = CreateTestUser("testuser", email);

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.TryGetByEmailAsync(email);

            // Assert
            result.Should().NotBeNull();
            result!.Email.Should().Be(email);
        }

        [Fact]
        public async Task TryGetByProviderAsync_WithExistingProvider_ShouldReturnUserDto()
        {
            // Arrange
            var provider = "google";
            var providerId = "provider123";
            var user = CreateTestUser("testuser", "test@example.com");
            user.Provider = provider;
            user.ProviderId = providerId;

            _mockUserRepository.Setup(x => x.GetByProviderAsync(provider, providerId))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.TryGetByProviderAsync(provider, providerId);

            // Assert
            result.Should().NotBeNull();
            result!.Provider.Should().Be(provider);
            result.ProviderId.Should().Be(providerId);
        }

        #endregion

        #region Exists Methods Tests

        [Fact]
        public async Task ExistsByIdAsync_WithExistingId_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser("testuser", "test@example.com", userId);

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.ExistsByIdAsync(userId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByIdAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((ProjectVG.Domain.Entities.Users.User?)null);

            // Act
            var result = await _userService.ExistsByIdAsync(userId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByEmailAsync_WithExistingEmail_ShouldReturnTrue()
        {
            // Arrange
            var email = "test@example.com";
            var user = CreateTestUser("testuser", email);

            _mockUserRepository.Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.ExistsByEmailAsync(email);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByUsernameAsync_WithExistingUsername_ShouldReturnTrue()
        {
            // Arrange
            var username = "testuser";
            var user = CreateTestUser(username, "test@example.com");

            _mockUserRepository.Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.ExistsByUsernameAsync(username);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByUidAsync_WithExistingUid_ShouldReturnTrue()
        {
            // Arrange
            var uid = "TEST123";
            var user = CreateTestUser("testuser", "test@example.com");
            user.UID = uid;

            _mockUserRepository.Setup(x => x.GetByUIDAsync(uid))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.ExistsByUidAsync(uid);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region Helper Methods

        private static ProjectVG.Domain.Entities.Users.User CreateTestUser(string username, string email, Guid? id = null, string provider = "google", string providerId = "provider123")
        {
            return new ProjectVG.Domain.Entities.Users.User
            {
                Id = id ?? Guid.NewGuid(),
                UID = "TEST123",
                Username = username,
                Email = email,
                Provider = provider,
                ProviderId = providerId,
                Status = AccountStatus.Active
            };
        }

        #endregion
    }
}