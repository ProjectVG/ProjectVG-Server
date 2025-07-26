using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace ProjectVG.Api.Configuration
{
    public class EnvironmentVariableConfigurationProvider : ConfigurationProvider
    {
        private readonly IConfiguration _configuration;

        public EnvironmentVariableConfigurationProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override void Load()
        {
            var data = new Dictionary<string, string>();
            
            // 모든 설정을 순회하면서 환경 변수 참조를 찾아서 치환
            ReplaceEnvironmentVariables(_configuration, data, "");
            
            Data = data;
        }

        private void ReplaceEnvironmentVariables(IConfiguration configuration, Dictionary<string, string> data, string prefix)
        {
            foreach (var child in configuration.GetChildren())
            {
                var key = string.IsNullOrEmpty(prefix) ? child.Key : $"{prefix}:{child.Key}";
                
                if (child.Value != null)
                {
                    var value = ReplaceEnvironmentVariableReferences(child.Value);
                    data[key] = value;
                }
                else
                {
                    // 하위 설정이 있으면 재귀적으로 처리
                    ReplaceEnvironmentVariables(child, data, key);
                }
            }
        }

        private string ReplaceEnvironmentVariableReferences(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // ${ENV_VAR} 패턴을 찾아서 환경 변수로 치환
            var pattern = @"\$\{([^}]+)\}";
            return Regex.Replace(value, pattern, match =>
            {
                var envVarName = match.Groups[1].Value;
                var envValue = Environment.GetEnvironmentVariable(envVarName);
                
                if (envValue != null)
                {
                    return envValue;
                }
                
                // 환경 변수가 없으면 원본 값 유지
                return match.Value;
            });
        }
    }

    public static class EnvironmentVariableConfigurationExtensions
    {
        public static IConfigurationBuilder AddEnvironmentVariableSubstitution(this IConfigurationBuilder builder, IConfiguration configuration)
        {
            return builder.Add(new EnvironmentVariableConfigurationSource(configuration));
        }
    }

    public class EnvironmentVariableConfigurationSource : IConfigurationSource
    {
        private readonly IConfiguration _configuration;

        public EnvironmentVariableConfigurationSource(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new EnvironmentVariableConfigurationProvider(_configuration);
        }
    }
} 