using System.Text.Json.Serialization;

namespace ProjectVG.Domain.Models.MessageBus
{
    /// <summary>
    /// 분산 메시지 버스를 통해 전달되는 메시지의 기본 클래스
    /// </summary>
    public abstract class DistributedMessage
    {
        [JsonPropertyName("messageId")]
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("messageType")]
        public abstract string MessageType { get; }

        [JsonPropertyName("fromServer")]
        public string FromServer { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }
    }

    /// <summary>
    /// WebSocket 메시지 전달 메시지
    /// </summary>
    public class WebSocketMessage : DistributedMessage
    {
        public override string MessageType => "websocket_message";

        [JsonPropertyName("targetUserId")]
        public string TargetUserId { get; set; } = string.Empty;

        [JsonPropertyName("payload")]
        public object Payload { get; set; } = new();

        [JsonPropertyName("messageFormat")]
        public WebSocketMessageFormat Format { get; set; } = WebSocketMessageFormat.Json;
    }

    /// <summary>
    /// 서버 상태 알림 메시지
    /// </summary>
    public class ServerStatusMessage : DistributedMessage
    {
        public override string MessageType => "server_status";

        [JsonPropertyName("serverId")]
        public string ServerId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // Online, Offline, Maintenance

        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// 채팅 결과 알림 메시지
    /// </summary>
    public class ChatResultMessage : DistributedMessage
    {
        public override string MessageType => "chat_result";

        [JsonPropertyName("targetUserId")]
        public string TargetUserId { get; set; } = string.Empty;

        [JsonPropertyName("conversationId")]
        public string ConversationId { get; set; } = string.Empty;

        [JsonPropertyName("messageId")]
        public new string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("audioData")]
        public byte[]? AudioData { get; set; }

        [JsonPropertyName("cost")]
        public double Cost { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 세션 상태 변경 메시지
    /// </summary>
    public class SessionUpdateMessage : DistributedMessage
    {
        public override string MessageType => "session_update";

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("serverId")]
        public string ServerId { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty; // Connect, Disconnect, Update

        [JsonPropertyName("sessionData")]
        public Dictionary<string, object> SessionData { get; set; } = new();
    }

    public enum WebSocketMessageFormat
    {
        Json,
        Binary,
        Text
    }
}