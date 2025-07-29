using System.Text.Json.Serialization;

namespace ProjectVG.Application.Models.Chat
{
    /// <summary>
    /// WebSocket 메시지 기본 구조
    /// </summary>
    public class WebSocketMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonPropertyName("data")]
        public object Data { get; set; } = new();
        
        public WebSocketMessage() { }
        
        public WebSocketMessage(string type, object data)
        {
            Type = type;
            Data = data;
        }
    }
} 