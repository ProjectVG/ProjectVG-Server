using System.Text.Json.Serialization;

namespace MainAPI_Server.Models.External.MemoryStore
{
    public class MemorySearchRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = default!;

        [JsonPropertyName("top_k")]
        public int TopK { get; set; } = 3;
    }
}
