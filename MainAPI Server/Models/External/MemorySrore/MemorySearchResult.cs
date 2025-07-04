
using System.Text.Json.Serialization;

namespace MainAPI_Server.Models.External.MemorySrore
{
    public class MemorySearchResult
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = default!;

        [JsonPropertyName("score")]
        public float Score { get; set; }
    }
}