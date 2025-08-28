using System.Text.Json.Serialization;

namespace ProjectVG.Infrastructure.Integrations.LLMClient.Models
{
    public class History
    {
        /// <summary>
        /// 메시지 역할 (user, assistant, system)
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        /// <summary>
        /// 메시지 내용
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }
}