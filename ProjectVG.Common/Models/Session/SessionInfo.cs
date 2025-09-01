namespace ProjectVG.Common.Models.Session
{
	public record SessionInfo
	{
		public string SessionId { get; init; } = string.Empty;
		public string? UserId { get; init; }
		public DateTime ConnectedAt { get; init; } = DateTime.UtcNow;
	}
}



