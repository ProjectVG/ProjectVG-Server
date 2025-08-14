using System.Text.Json.Serialization;

namespace ProjectVG.Infrastructure.Integrations.MemoryClient.Models
{
    public class MemorySearchRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = default!;

        [JsonPropertyName("top_k")]
        public int TopK { get; set; } = 3;

        [JsonPropertyName("time_weight")]
        public float TimeWeight { get; set; } = 0.3f;

        [JsonPropertyName("reference_time")]
        public string? ReferenceTime { get; set; }

        [JsonPropertyName("collection")]
        public string Collection { get; set; } = string.Empty;
    }
} 