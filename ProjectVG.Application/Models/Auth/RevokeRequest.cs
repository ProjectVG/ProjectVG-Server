namespace ProjectVG.Application.Models.Auth;

public class RevokeRequest
{
    public string Token { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? TokenTypeHint { get; set; }
}
