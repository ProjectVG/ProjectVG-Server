using ProjectVG.Common.Models;

namespace ProjectVG.Infrastructure.Auth
{
    public interface ITokenService
    {
        /// <summary>   
        /// 토큰 생성
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <summary>
/// 지정된 사용자 ID에 대한 액세스 토큰과 리프레시 토큰을 생성합니다.
/// </summary>
/// <param name="userId">토큰을 생성할 대상 사용자의 식별자(Guid).</param>
/// <returns>생성된 액세스 토큰 및 리프레시 토큰을 포함한 TokenResponse.</returns>
        Task<TokenResponse> GenerateTokensAsync(Guid userId);

        /// <summary>
        /// 리프레시 토큰으로 액세스 토큰 갱신
        /// </summary>
        /// <param name="refreshToken">리프레시 토큰</param>
        /// <summary>
/// 주어진 리프레시 토큰으로 새 액세스(및 리프레시) 토큰을 발급합니다.
/// </summary>
/// <param name="refreshToken">갱신에 사용할 리프레시 토큰 문자열.</param>
/// <returns>성공하면 새로 발급된 TokenResponse 객체를 반환합니다. 리프레시 토큰이 유효하지 않거나 갱신할 수 없으면 null을 반환합니다.</returns>
        Task<TokenResponse?> RefreshAccessTokenAsync(string refreshToken);

        /// <summary>
        /// 리프레시 토큰 무효화
        /// </summary>
        /// <param name="refreshToken">무효화할 리프레시 토큰</param>
        /// <summary>
/// 지정한 리프레시 토큰을 무효화하여 이후 재사용을 방지합니다.
/// </summary>
/// <param name="refreshToken">무효화할 리프레시 토큰 문자열.</param>
/// <returns>무효화에 성공하면 true, 토큰이 존재하지 않거나 무효화에 실패하면 false.</returns>
        Task<bool> RevokeRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// 리프레시 토큰 유효성 검사
        /// </summary>
        /// <param name="refreshToken">검사할 리프레시 토큰</param>
        /// <summary>
/// 주어진 리프레시 토큰이 현재 유효한지 확인합니다.
/// </summary>
/// <param name="refreshToken">검증할 리프레시 토큰 문자열.</param>
/// <returns>토큰이 유효하면 <c>true</c>, 무효(만료·취소·변조 등)하면 <c>false</c>를 반환합니다.</returns>
        Task<bool> ValidateRefreshTokenAsync(string refreshToken);

        /// <summary>
        /// 액세스 토큰 유효성 검증
        /// </summary>
        /// <param name="accessToken">검증할 액세스 토큰</param>
        /// <summary>
/// 주어진 액세스 토큰의 유효성을 검증합니다.
/// </summary>
/// <param name="accessToken">검증할 액세스 토큰(JWT 등)의 문자열 표현.</param>
/// <returns>토큰이 유효하면 <c>true</c>, 유효하지 않거나 만료되었으면 <c>false</c>를 반환합니다.</returns>
        Task<bool> ValidateAccessTokenAsync(string accessToken);

        /// <summary>
        /// 토큰에서 사용자 ID 추출
        /// </summary>
        /// <param name="token">사용자 ID를 추출할 토큰</param>
        /// <summary>
/// 토큰에서 사용자 식별자(Guid)를 추출합니다.
/// </summary>
/// <param name="token">사용자 ID를 포함하고 있는 유효한 액세스 또는 리프레시 토큰 문자열.</param>
/// <returns>추출된 사용자 ID(Guid). 토큰이 유효하지 않거나 ID를 추출할 수 없으면 null을 반환합니다.</returns>
        Task<Guid?> GetUserIdFromTokenAsync(string token);
    }
}
