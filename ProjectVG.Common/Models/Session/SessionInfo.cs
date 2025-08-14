namespace ProjectVG.Common.Models.Session
{
	public class SessionInfo
	{
		public string SessionId { get; set; } = string.Empty;
		public string? UserId { get; set; }
		public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
	}
}



