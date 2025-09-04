using System.Text.Json.Serialization;

namespace ProjectVG.Application.Models.WebSocket
{
    public record WebSocketMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;
        
        [JsonPropertyName("message_type")]
        public string MessageType { get; init; } = "json";
        
        [JsonPropertyName("data")]
        public object Data { get; init; } = new();
        
        public WebSocketMessage() { }
        
        public WebSocketMessage(string type, object data, string messageType = "json")
        {
            Type = type;
            MessageType = messageType;
            Data = data;
        }
    }
}
