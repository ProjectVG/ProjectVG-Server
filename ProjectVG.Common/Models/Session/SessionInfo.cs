namespace ProjectVG.Common.Models.Session
{
	public class SessionInfo
	{
		public required string SessionId { get; set; }
		public string? UserId { get; set; }
		public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
		public DateTime LastActivity { get; set; } = DateTime.UtcNow;
	}
}



