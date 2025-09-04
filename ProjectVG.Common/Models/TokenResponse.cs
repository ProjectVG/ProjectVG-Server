namespace ProjectVG.Common.Models
{
    public record TokenResponse
    {
        public required string AccessToken { get; init; }
        public required string RefreshToken { get; init; }
        public DateTime AccessTokenExpiresAt { get; init; }
        public DateTime RefreshTokenExpiresAt { get; init; }
    }
}
