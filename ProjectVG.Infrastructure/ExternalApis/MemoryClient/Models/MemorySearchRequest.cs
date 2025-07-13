using System.Text.Json.Serialization;

namespace ProjectVG.Infrastructure.ExternalApis.MemoryStore.Models
{
    public class MemorySearchRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = default!;

        [JsonPropertyName("top_k")]
        public int TopK { get; set; } = 3;
    }
} 