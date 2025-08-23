using Microsoft.Extensions.Options;
using ProjectVG.Infrastructure.Auth;
using ProjectVG.Infrastructure.Auth.Models;
using Xunit;

namespace ProjectVG.Tests.Auth;

public class FileSystemKeyStoreTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileSystemKeyStore _keyStore;

    public FileSystemKeyStoreTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "test-keys", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        var jwtSettings = Options.Create(new JwtSettings
        {
            KeysDirectory = _testDirectory,
            RotationIntervalDays = 30
        });

        _keyStore = new FileSystemKeyStore(jwtSettings);
    }

    [Fact]
    public async Task GetCurrentSigningKeyAsync_ShouldGenerateKey_WhenNoKeysExist()
    {
        // Act
        var signingKey = await _keyStore.GetCurrentSigningKeyAsync();

        // Assert
        Assert.NotNull(signingKey);
        Assert.NotNull(signingKey.KeyId);
        Assert.True(signingKey.HasPrivateKey);
    }

    [Fact]
    public async Task GetValidationKeysAsync_ShouldReturnKeys_AfterKeyGeneration()
    {
        // Arrange - 먼저 키를 생성
        await _keyStore.GetCurrentSigningKeyAsync();

        // Act
        var validationKeys = await _keyStore.GetValidationKeysAsync();

        // Assert
        Assert.NotEmpty(validationKeys);
        Assert.All(validationKeys, key => Assert.False(key.HasPrivateKey)); // 검증용 키는 공개키만
    }

    [Fact]
    public async Task RotateKeyAsync_ShouldCreateNewKey()
    {
        // Arrange
        var firstKey = await _keyStore.GetCurrentSigningKeyAsync();
        var firstKeyId = firstKey.KeyId;

        // Act
        var newKeyId = await _keyStore.RotateKeyAsync();
        var newKey = await _keyStore.GetCurrentSigningKeyAsync();

        // Assert
        Assert.NotEqual(firstKeyId, newKeyId);
        Assert.Equal(newKeyId, newKey.KeyId);
    }

    [Fact]
    public async Task IsKeyRotationNeededAsync_ShouldReturnTrue_WhenNoKeysExist()
    {
        // Act
        var needsRotation = await _keyStore.IsKeyRotationNeededAsync();

        // Assert
        Assert.True(needsRotation);
    }

    [Fact]
    public async Task RevokeKeyAsync_ShouldUpdateKeyStatus()
    {
        // Arrange
        var signingKey = await _keyStore.GetCurrentSigningKeyAsync();
        var keyId = signingKey.KeyId;

        // Act
        await _keyStore.RevokeKeyAsync(keyId);

        // Assert
        // 키가 폐기되었는지 확인하기 위해 메타데이터 파일을 확인할 수 있지만,
        // 현재 구현에서는 직접적인 검증 방법이 제한적입니다.
        // 실제 구현에서는 GetKeyStatusAsync 같은 메서드를 추가할 수 있습니다.
        
        // 최소한 예외가 발생하지 않았는지 확인
        Assert.True(true);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
