using System.Text.Json.Serialization;

namespace MainAPI_Server.Models.External.MemoryStore
{
    public class MemoryAddRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = default!;

        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}