namespace ProjectVG.Common.Models.Session
{
	public record SessionInfo
	{
		public required string SessionId { get; init; }
		public string? UserId { get; init; }
		public DateTime ConnectedAt { get; init; } = DateTime.UtcNow;
	}
}



