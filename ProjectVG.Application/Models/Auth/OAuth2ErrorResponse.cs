namespace ProjectVG.Application.Models.Auth;

public class OAuth2ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? ErrorDescription { get; set; }
    public string? ErrorUri { get; set; }
    public string? State { get; set; }
}
