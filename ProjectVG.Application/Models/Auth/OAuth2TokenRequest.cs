using System.ComponentModel.DataAnnotations;

namespace ProjectVG.Application.Models.Auth;

public class OAuth2TokenRequest
{
    [Required]
    public string GrantType { get; set; } = string.Empty; // "authorization_code"

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string? RedirectUri { get; set; }

    [Required]
    public string CodeVerifier { get; set; } = string.Empty;
}
