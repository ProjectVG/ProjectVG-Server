using System.Text.Json.Serialization;

namespace ProjectVG.Application.Models.WebSocket
{
    public record WebSocketMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;
        
        [JsonPropertyName("data")]
        public object Data { get; init; } = new();
        
        public WebSocketMessage() { }
        
        public WebSocketMessage(string type, object data)
        {
            Type = type;
            Data = data;
        }
    }
}
