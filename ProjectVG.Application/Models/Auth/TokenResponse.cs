namespace ProjectVG.Application.Models.Auth;

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; } = 900; // 15분
    public string? Scope { get; set; }
    
    // AuthTokenService를 위한 추가 프로퍼티
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
