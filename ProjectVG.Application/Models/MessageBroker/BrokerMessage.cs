using System.Text.Json;

namespace ProjectVG.Application.Models.MessageBroker
{
    public class BrokerMessage
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string MessageType { get; set; } = string.Empty;
        public string? TargetUserId { get; set; }
        public string? TargetServerId { get; set; }
        public string? SourceServerId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Payload { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();

        public static BrokerMessage CreateUserMessage(string userId, object payload, string? sourceServerId = null)
        {
            return new BrokerMessage
            {
                MessageType = "user_message",
                TargetUserId = userId,
                SourceServerId = sourceServerId,
                Payload = JsonSerializer.Serialize(payload),
                Headers = new Dictionary<string, string>
                {
                    ["content-type"] = "application/json"
                }
            };
        }

        public static BrokerMessage CreateServerMessage(string serverId, object payload, string? sourceServerId = null)
        {
            return new BrokerMessage
            {
                MessageType = "server_message",
                TargetServerId = serverId,
                SourceServerId = sourceServerId,
                Payload = JsonSerializer.Serialize(payload),
                Headers = new Dictionary<string, string>
                {
                    ["content-type"] = "application/json"
                }
            };
        }

        public static BrokerMessage CreateBroadcastMessage(object payload, string? sourceServerId = null)
        {
            return new BrokerMessage
            {
                MessageType = "broadcast_message",
                SourceServerId = sourceServerId,
                Payload = JsonSerializer.Serialize(payload),
                Headers = new Dictionary<string, string>
                {
                    ["content-type"] = "application/json"
                }
            };
        }

        public T? DeserializePayload<T>()
        {
            try
            {
                return JsonSerializer.Deserialize<T>(Payload);
            }
            catch
            {
                return default;
            }
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static BrokerMessage? FromJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<BrokerMessage>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}