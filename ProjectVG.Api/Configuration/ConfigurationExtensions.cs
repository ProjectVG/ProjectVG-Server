using Microsoft.Extensions.Configuration;

namespace ProjectVG.Api.Configuration
{
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// 환경 변수 우선, 설정 파일 차선으로 값을 가져옵니다.
        /// </summary>
        /// <param name="configuration">Configuration 인스턴스</param>
        /// <param name="configKey">설정 파일 키</param>
        /// <param name="envVarName">환경 변수 이름</param>
        /// <param name="defaultValue">기본값</param>
        /// <returns>설정 값</returns>
        public static string GetValueWithEnvPriority(
            this IConfiguration configuration, 
            string configKey, 
            string envVarName, 
            string defaultValue = "")
        {
            // 1. 환경 변수 우선
            var envValue = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                return envValue;
            }

            // 2. 설정 파일 차선
            var configValue = configuration[configKey];
            if (!string.IsNullOrWhiteSpace(configValue))
            {
                return configValue;
            }

            // 3. 기본값
            return defaultValue;
        }

        /// <summary>
        /// 필수 설정 값을 가져옵니다. 값이 없으면 예외를 발생시킵니다.
        /// </summary>
        /// <param name="configuration">Configuration 인스턴스</param>
        /// <param name="configKey">설정 파일 키</param>
        /// <param name="envVarName">환경 변수 이름</param>
        /// <param name="settingName">설정 이름 (오류 메시지용)</param>
        /// <returns>설정 값</returns>
        public static string GetRequiredValue(
            this IConfiguration configuration, 
            string configKey, 
            string envVarName, 
            string settingName)
        {
            var value = configuration.GetValueWithEnvPriority(configKey, envVarName);
            
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"{settingName}이 설정되지 않았습니다. " +
                    $"환경 변수 '{envVarName}' 또는 설정 파일 '{configKey}'에서 설정해주세요.");
            }
            
            return value;
        }
    }
} 