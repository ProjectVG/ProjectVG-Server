using System.ComponentModel.DataAnnotations;

namespace ProjectVG.Application.Models.Auth;

public class OAuth2AuthorizeRequest
{
    [Required]
    public string ResponseType { get; set; } = string.Empty; // "code"

    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string? RedirectUri { get; set; }

    public string? Scope { get; set; } = "openid profile";

    public string? State { get; set; }

    [Required]
    public string CodeChallenge { get; set; } = string.Empty;

    [Required]
    public string CodeChallengeMethod { get; set; } = string.Empty; // "S256"
}
