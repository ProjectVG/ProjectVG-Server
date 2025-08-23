using ProjectVG.Application.Models.Auth;

namespace ProjectVG.Application.Services.Auth;

/// <summary>
/// 토큰 갱신 및 폐기를 위한 고수준 서비스
/// </summary>
public interface IAuthTokenService
{
    /// <summary>
    /// 리프레시 토큰을 사용하여 새로운 토큰 세트 발급
    /// </summary>
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);
    
    /// <summary>
    /// 토큰 폐기 (리프레시 또는 액세스 토큰)
    /// </summary>
    Task<(bool IsSuccess, string? ErrorMessage)> RevokeTokenAsync(string token);
    
    /// <summary>
    /// 특정 사용자의 모든 토큰 폐기
    /// </summary>
    Task RevokeAllUserTokensAsync(Guid userId);
}
