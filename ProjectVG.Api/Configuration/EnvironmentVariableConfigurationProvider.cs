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

        /// <summary>
        /// IConfiguration의 값을 순회하여 `${ENV_VAR}` 형식의 참조를 운영체제 환경변수 값으로 치환한 후 프로바이더의 Data로 설정합니다.
        /// </summary>
        /// <remarks>
        /// - 내부적으로 키-값 쌍을 Dictionary&lt;string, string?&gt;에 수집하고, ReplaceEnvironmentVariables를 재귀 호출하여 모든 섹션을 처리합니다.
        /// - 치환 시 환경변수가 존재하지 않으면 원래의 `${...}` 플레이스홀더를 그대로 유지합니다.
        /// - 실행 결과는 이 인스턴스의 Data 프로퍼티에 할당됩니다.
        /// </remarks>
        public override void Load()
        {
            var data = new Dictionary<string, string?>();
            
            // 모든 설정을 순회하면서 환경 변수 참조를 찾아서 치환
            ReplaceEnvironmentVariables(_configuration, data, "");
            
            Data = data;
        }

        /// <summary>
        /// IConfiguration 트리의 모든 리프 값을 순회하여 환경 변수 참조(`${NAME}`)를 치환한 후 계층형 키(콜론 구분)로 데이터 사전에 저장합니다.
        /// </summary>
        /// <param name="configuration">대상 IConfiguration 섹션(또는 루트). 이 함수는 해당 섹션의 모든 자식 항목을 재귀적으로 처리합니다.</param>
        /// <param name="data">계층형 키를 키로 하고 치환된(또는 원본 플레이스홀더가 남아있는) 문자열 값을 값으로 저장하는 출력 사전(값은 nullable).</param>
        /// <param name="prefix">현재 계층 키 프리픽스(빈 문자열이면 최상위). 자식 키는 "{prefix}:{childKey}" 형태로 구성됩니다.</param>
        /// <remarks>
        /// - 리프 노드(값이 null이 아닌 항목)의 값은 ReplaceEnvironmentVariableReferences를 통해 환경 변수 참조가 치환됩니다.
        /// - 섹션(값이 null인 항목)은 재귀적으로 탐색하여 모든 하위 항목을 처리합니다.
        /// - 이 메서드는 data 사전을 변경하는 부작용이 있습니다.
        /// </remarks>
        private void ReplaceEnvironmentVariables(IConfiguration configuration, Dictionary<string, string?> data, string prefix)
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