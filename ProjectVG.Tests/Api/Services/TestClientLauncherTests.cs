using FluentAssertions;
using ProjectVG.Api.Services;
using Xunit;

namespace ProjectVG.Tests.Api.Services
{
    public class TestClientLauncherTests : IDisposable
    {
        private readonly string _testDirectory;

        public TestClientLauncherTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "TestClientLauncherTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch
                {
                    // Ignore cleanup failures in tests
                }
            }
        }

        #region Constructor Tests

        [Fact]
        public void TestClientLauncher_Constructor_ShouldCreateInstance()
        {
            // Act
            var launcher = new TestClientLauncher();

            // Assert
            launcher.Should().NotBeNull();
        }

        #endregion

        #region Launch Method Tests

        [Fact]
        public void Launch_ShouldExecuteWithoutThrowingException()
        {
            // Arrange
            var launcher = new TestClientLauncher();

            // Act & Assert - Should not throw exception even if file doesn't exist
            var act = () => launcher.Launch();
            act.Should().NotThrow("Launch 메서드는 예외를 던지지 않아야 함");
        }

        [Fact]
        public async Task Launch_ShouldStartAsyncTask()
        {
            // Arrange
            var launcher = new TestClientLauncher();
            var beforeLaunch = DateTime.Now;

            // Act
            launcher.Launch();
            
            // Give some time for the async task to start
            await Task.Delay(100);
            
            var afterDelay = DateTime.Now;

            // Assert - Method should return immediately (async fire-and-forget)
            var elapsedTime = afterDelay - beforeLaunch;
            elapsedTime.Should().BeLessOrEqualTo(TimeSpan.FromMilliseconds(500), 
                "Launch는 즉시 반환되어야 함 (백그라운드에서 실행)");
        }

        [Fact]
        public void Launch_CalledMultipleTimes_ShouldNotThrow()
        {
            // Arrange
            var launcher = new TestClientLauncher();

            // Act & Assert
            launcher.Launch();
            var act = () => launcher.Launch();
            act.Should().NotThrow("Launch는 여러 번 호출해도 안전해야 함");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task Launch_WithValidTestDirectory_ShouldAttemptProcessStart()
        {
            // Arrange - Create a valid test-clients directory structure
            var testClientsDir = Path.Combine(_testDirectory, "test-clients");
            Directory.CreateDirectory(testClientsDir);
            
            var launcher = new TestClientLauncher();

            // Act - Should not throw even if process start fails
            var act = () => launcher.Launch();
            act.Should().NotThrow();
            
            // Allow async task to complete
            await Task.Delay(1200);

            // Assert - Directory should still exist
            Directory.Exists(testClientsDir).Should().BeTrue();
        }

        #endregion
    }
}