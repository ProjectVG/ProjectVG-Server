using System.Net.WebSockets;

public class ClientConnection
{
    public string SessionId { get; set; }
    public string? UserId { get; set; }
    public WebSocket Socket { get; set; }
    public DateTime ConnectedAt { get; set; }
}