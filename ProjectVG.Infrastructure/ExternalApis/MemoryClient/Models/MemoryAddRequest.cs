using System.Text.Json.Serialization;

namespace ProjectVG.Infrastructure.ExternalApis.MemoryClient.Models
{
    public class MemoryAddRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = default!;

        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("collection")]
        public string Collection { get; set; } = string.Empty;
    }
} 