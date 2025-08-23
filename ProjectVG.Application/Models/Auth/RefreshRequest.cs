namespace ProjectVG.Application.Models.Auth;

public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}
