using System.Security.Claims;

namespace ProjectVG.Application.Models.Auth;

public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public ClaimsPrincipal? Principal { get; set; }
    public Guid? UserId { get; set; }
    public string? ClientId { get; set; }
    public string? DeviceId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<Claim>? Claims { get; set; }
}
