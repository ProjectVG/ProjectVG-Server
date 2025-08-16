namespace ProjectVG.Common.Models.Session
{
	public interface IClientConnection
	{
		string SessionId { get; }
		string? UserId { get; }
		DateTime ConnectedAt { get; }
		Task SendTextAsync(string message);
		Task SendBinaryAsync(byte[] data);
	}
}


