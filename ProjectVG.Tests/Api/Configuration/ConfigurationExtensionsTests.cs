using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ProjectVG.Api.Configuration;
using Xunit;

namespace ProjectVG.Tests.Api.Configuration
{
    public class ConfigurationExtensionsTests : IDisposable
    {
        private readonly List<string> _environmentVariablesToCleanup = new();

        public void Dispose()
        {
            // Clean up environment variables set during tests
            foreach (var envVar in _environmentVariablesToCleanup)
            {
                Environment.SetEnvironmentVariable(envVar, null);
            }
        }

        #region GetValueWithEnvPriority Tests

        [Fact]
        public void GetValueWithEnvPriority_EnvironmentVariableExists_ShouldReturnEnvironmentVariableValue()
        {
            // Arrange
            var envVarName = "TEST_ENV_VAR_1";
            var configKey = "TestConfig:Key1";
            var envValue = "environment-value";
            var configValue = "config-value";
            var defaultValue = "default-value";

            SetEnvironmentVariable(envVarName, envValue);
            
            var configData = new Dictionary<string, string?>
            {
                [configKey] = configValue
            };
            var configuration = CreateConfiguration(configData);

            // Act
            var result = configuration.GetValueWithEnvPriority(configKey, envVarName, defaultValue);

            // Assert
            result.Should().Be(envValue, "환경 변수가 최우선이어야 함");
        }

        [Fact]
        public void GetValueWithEnvPriority_EnvironmentVariableEmpty_ConfigExists_ShouldReturnConfigValue()
        {
            // Arrange
            var envVarName = "TEST_ENV_VAR_2";
            var configKey = "TestConfig:Key2";
            var configValue = "config-value";
            var defaultValue = "default-value";

            // Environment variable not set
            var configData = new Dictionary<string, string?>
            {
                [configKey] = configValue
            };
            var configuration = CreateConfiguration(configData);

            // Act
            var result = configuration.GetValueWithEnvPriority(configKey, envVarName, defaultValue);

            // Assert
            result.Should().Be(configValue, "환경 변수가 없을 때 설정 파일 값을 사용해야 함");
        }

        [Fact]
        public void GetValueWithEnvPriority_BothEmpty_ShouldReturnDefaultValue()
        {
            // Arrange
            var envVarName = "TEST_ENV_VAR_3";
            var configKey = "TestConfig:Key3";
            var defaultValue = "default-value";

            var configuration = CreateConfiguration(new Dictionary<string, string?>());

            // Act
            var result = configuration.GetValueWithEnvPriority(configKey, envVarName, defaultValue);

            // Assert
            result.Should().Be(defaultValue, "둘 다 없을 때 기본값을 사용해야 함");
        }

        [Fact]
        public void GetValueWithEnvPriority_EnvironmentVariableWhitespaceOnly_ConfigExists_ShouldReturnConfigValue()
        {
            // Arrange
            var envVarName = "TEST_ENV_VAR_4";
            var configKey = "TestConfig:Key4";
            var configValue = "config-value";
            var defaultValue = "default-value";

            SetEnvironmentVariable(envVarName, "   "); // Whitespace only
            
            var configData = new Dictionary<string, string?>
            {
                [configKey] = configValue
            };
            var configuration = CreateConfiguration(configData);

            // Act
            var result = configuration.GetValueWithEnvPriority(configKey, envVarName, defaultValue);

            // Assert
            result.Should().Be(configValue, "환경 변수가 공백만 있을 때 설정 파일 값을 사용해야 함");
        }

        [Fact]
        public void GetValueWithEnvPriority_ConfigValueWhitespaceOnly_ShouldReturnDefaultValue()
        {
            // Arrange
            var envVarName = "TEST_ENV_VAR_5";
            var configKey = "TestConfig:Key5";
            var defaultValue = "default-value";

            var configData = new Dictionary<string, string?>
            {
                [configKey] = "   " // Whitespace only
            };
            var configuration = CreateConfiguration(configData);

            // Act
            var result = configuration.GetValueWithEnvPriority(configKey, envVarName, defaultValue);

            // Assert
            result.Should().Be(defaultValue, "설정 파일 값이 공백만 있을 때 기본값을 사용해야 함");
        }

        [Fact]
        public void GetValueWithEnvPriority_BothNull_ShouldReturnDefaultValue()
        {
            // Arrange
            var envVarName = "TEST_ENV_VAR_6";
            var configKey = "TestConfig:Key6";
            var defaultValue = "default-value";

            var configData = new Dictionary<string, string?>
            {
                [configKey] = null
            };
            var configuration = CreateConfiguration(configData);

            // Act
            var result = configuration.GetValueWithEnvPriority(configKey, envVarName, defaultValue);

            // Assert
            result.Should().Be(defaultValue, "둘 다 null일 때 기본값을 사용해야 함");
        }

        [Fact]
        public void GetValueWithEnvPriority_EmptyDefaultValue_ShouldReturnEmptyString()
        {
            // Arrange
            var envVarName = "TEST_ENV_VAR_7";
            var configKey = "TestConfig:Key7";
            var defaultValue = "";

            var configuration = CreateConfiguration(new Dictionary<string, string?>());

            // Act
            var result = configuration.GetValueWithEnvPriority(configKey, envVarName, defaultValue);

            // Assert
            result.Should().Be("", "기본값이 빈 문자열일 때 빈 문자열을 반환해야 함");
        }

        [Fact]
        public void GetValueWithEnvPriority_NoDefaultValueProvided_ShouldReturnEmptyString()
        {
            // Arrange
            var envVarName = "TEST_ENV_VAR_8";
            var configKey = "TestConfig:Key8";

            var configuration = CreateConfiguration(new Dictionary<string, string?>());

            // Act
            var result = configuration.GetValueWithEnvPriority(configKey, envVarName);

            // Assert
            result.Should().Be("", "기본값을 제공하지 않으면 빈 문자열을 반환해야 함");
        }

        [Fact]
        public void GetValueWithEnvPriority_EnvironmentVariableOverridesConfig_CaseSensitive()
        {
            // Arrange
            var envVarName = "TEST_ENV_VAR_9";
            var configKey = "TestConfig:Key9";
            var envValue = "ENV_VALUE";
            var configValue = "config_value";

            SetEnvironmentVariable(envVarName, envValue);
            
            var configData = new Dictionary<string, string?>
            {
                [configKey] = configValue
            };
            var configuration = CreateConfiguration(configData);

            // Act
            var result = configuration.GetValueWithEnvPriority(configKey, envVarName);

            // Assert
            result.Should().Be(envValue, "환경 변수가 설정 파일을 오버라이드해야 함");
        }

        #endregion

        #region GetRequiredValue Tests

        [Fact]
        public void GetRequiredValue_EnvironmentVariableExists_ShouldReturnEnvironmentVariableValue()
        {
            // Arrange
            var envVarName = "TEST_REQUIRED_ENV_1";
            var configKey = "TestConfig:RequiredKey1";
            var settingName = "Test Required Setting";
            var envValue = "required-env-value";

            SetEnvironmentVariable(envVarName, envValue);
            var configuration = CreateConfiguration(new Dictionary<string, string?>());

            // Act
            var result = configuration.GetRequiredValue(configKey, envVarName, settingName);

            // Assert
            result.Should().Be(envValue, "필수 설정에서 환경 변수 값을 반환해야 함");
        }

        [Fact]
        public void GetRequiredValue_ConfigExists_ShouldReturnConfigValue()
        {
            // Arrange
            var envVarName = "TEST_REQUIRED_ENV_2";
            var configKey = "TestConfig:RequiredKey2";
            var settingName = "Test Required Setting";
            var configValue = "required-config-value";

            var configData = new Dictionary<string, string?>
            {
                [configKey] = configValue
            };
            var configuration = CreateConfiguration(configData);

            // Act
            var result = configuration.GetRequiredValue(configKey, envVarName, settingName);

            // Assert
            result.Should().Be(configValue, "필수 설정에서 설정 파일 값을 반환해야 함");
        }

        [Fact]
        public void GetRequiredValue_BothEmpty_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var envVarName = "TEST_REQUIRED_ENV_3";
            var configKey = "TestConfig:RequiredKey3";
            var settingName = "Test Required Setting";

            var configuration = CreateConfiguration(new Dictionary<string, string?>());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => configuration.GetRequiredValue(configKey, envVarName, settingName)
            );

            exception.Message.Should().Contain(settingName);
            exception.Message.Should().Contain(envVarName);
            exception.Message.Should().Contain(configKey);
            exception.Message.Should().Contain("설정되지 않았습니다");
        }

        [Fact]
        public void GetRequiredValue_WhitespaceValues_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var envVarName = "TEST_REQUIRED_ENV_4";
            var configKey = "TestConfig:RequiredKey4";
            var settingName = "Test Required Setting";

            SetEnvironmentVariable(envVarName, "   ");
            
            var configData = new Dictionary<string, string?>
            {
                [configKey] = "   "
            };
            var configuration = CreateConfiguration(configData);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => configuration.GetRequiredValue(configKey, envVarName, settingName)
            );

            exception.Message.Should().Contain(settingName);
        }

        [Fact]
        public void GetRequiredValue_NullValues_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var envVarName = "TEST_REQUIRED_ENV_5";
            var configKey = "TestConfig:RequiredKey5";
            var settingName = "Test Required Setting";

            var configData = new Dictionary<string, string?>
            {
                [configKey] = null
            };
            var configuration = CreateConfiguration(configData);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => configuration.GetRequiredValue(configKey, envVarName, settingName)
            );

            exception.Message.Should().Contain(settingName);
            exception.Message.Should().Contain("설정되지 않았습니다");
        }

        [Fact]
        public void GetRequiredValue_ValidValue_ShouldNotThrow()
        {
            // Arrange
            var envVarName = "TEST_REQUIRED_ENV_6";
            var configKey = "TestConfig:RequiredKey6";
            var settingName = "Test Required Setting";
            var validValue = "valid-required-value";

            SetEnvironmentVariable(envVarName, validValue);
            var configuration = CreateConfiguration(new Dictionary<string, string?>());

            // Act
            var result = configuration.GetRequiredValue(configKey, envVarName, settingName);

            // Assert
            result.Should().Be(validValue, "유효한 값이 있을 때 예외가 발생하지 않아야 함");
        }

        [Fact]
        public void GetRequiredValue_ExceptionMessage_ShouldContainAllRequiredInformation()
        {
            // Arrange
            var envVarName = "MISSING_ENV_VAR";
            var configKey = "MissingConfig:Key";
            var settingName = "Critical Database Connection";

            var configuration = CreateConfiguration(new Dictionary<string, string?>());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => configuration.GetRequiredValue(configKey, envVarName, settingName)
            );

            // Verify exception message contains all necessary information
            var message = exception.Message;
            message.Should().Contain(settingName, "설정 이름이 포함되어야 함");
            message.Should().Contain(envVarName, "환경 변수 이름이 포함되어야 함");
            message.Should().Contain(configKey, "설정 키가 포함되어야 함");
            message.Should().Contain("설정되지 않았습니다", "한국어 오류 메시지가 포함되어야 함");
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void GetValueWithEnvPriority_SpecialCharactersInValues_ShouldHandleCorrectly()
        {
            // Arrange
            var envVarName = "TEST_SPECIAL_CHARS";
            var configKey = "TestConfig:SpecialChars";
            var specialValue = "value with spaces, symbols: !@#$%^&*()_+-={}[]|\\:;\"'<>?,./";

            SetEnvironmentVariable(envVarName, specialValue);
            var configuration = CreateConfiguration(new Dictionary<string, string?>());

            // Act
            var result = configuration.GetValueWithEnvPriority(configKey, envVarName);

            // Assert
            result.Should().Be(specialValue, "특수 문자가 포함된 값을 올바르게 처리해야 함");
        }

        [Fact]
        public void GetValueWithEnvPriority_UnicodeValues_ShouldHandleCorrectly()
        {
            // Arrange
            var envVarName = "TEST_UNICODE";
            var configKey = "TestConfig:Unicode";
            var unicodeValue = "테스트 값 with émojis 🚀 and symbols ♥♦♣♠";

            SetEnvironmentVariable(envVarName, unicodeValue);
            var configuration = CreateConfiguration(new Dictionary<string, string?>());

            // Act
            var result = configuration.GetValueWithEnvPriority(configKey, envVarName);

            // Assert
            result.Should().Be(unicodeValue, "유니코드 문자가 포함된 값을 올바르게 처리해야 함");
        }

        [Fact]
        public void GetValueWithEnvPriority_VeryLongValues_ShouldHandleCorrectly()
        {
            // Arrange
            var envVarName = "TEST_LONG_VALUE";
            var configKey = "TestConfig:LongValue";
            var longValue = new string('A', 10000); // 10KB string

            SetEnvironmentVariable(envVarName, longValue);
            var configuration = CreateConfiguration(new Dictionary<string, string?>());

            // Act
            var result = configuration.GetValueWithEnvPriority(configKey, envVarName);

            // Assert
            result.Should().Be(longValue, "매우 긴 값을 올바르게 처리해야 함");
            result.Length.Should().Be(10000);
        }

        #endregion

        #region Helper Methods

        private void SetEnvironmentVariable(string name, string value)
        {
            Environment.SetEnvironmentVariable(name, value);
            _environmentVariablesToCleanup.Add(name);
        }

        private static IConfiguration CreateConfiguration(Dictionary<string, string?> data)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(data)
                .Build();
        }

        #endregion
    }
}