using System.Text.Json.Serialization;

namespace ProjectVG.Infrastructure.ExternalApis.MemoryClient.Models
{
    public class MemorySearchResult
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = default!;

        [JsonPropertyName("score")]
        public float Score { get; set; }
    }
} 