using ProjectVG.Common.Models;

namespace ProjectVG.Infrastructure.Auth
{
    public interface ITokenService
    {
        /// <summary>   
        /// 토큰 생성
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>생성된 토큰 정보</returns>
        Task<TokenResponse> GenerateTokensAsync(Guid userId);

        /// <summary>
        /// 리프레시 토큰으로 액세스 토큰 갱신
        /// </summary>
        /// <param name="refreshToken">리프레시 토큰</param>
        /// <returns>갱신된 토큰 정보</returns>
        Task<TokenResponse?> RefreshAccessTokenAsync(string refreshToken);

        /// <summary>
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// 리프레시 토큰 유효성 검사
        /// </summary>
        /// <param name="refreshToken">검사할 리프레시 토큰</param>
        /// <returns>유효성 검사 결과</returns>
        Task<bool> ValidateRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// 토큰에서 사용자 ID 추출
        /// </summary>
        /// <param name="token">사용자 ID를 추출할 토큰</param>
        /// <returns>추출된 사용자 ID</returns>
        Task<Guid?> GetUserIdFromTokenAsync(string token);
    }
}
