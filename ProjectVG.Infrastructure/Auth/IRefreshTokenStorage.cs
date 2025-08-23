namespace ProjectVG.Infrastructure.Auth
{
    public interface IRefreshTokenStorage
    {
        /// <summary>
        /// 리프레시 토큰 저장
        /// </summary>
        /// <param name="refreshToken">리프레시 토큰</param>
        /// <param name="userId">사용자 ID</param>
        /// <param name="expiresAt">토큰 만료 시간</param>
        /// <returns>저장 성공 여부</returns>
        Task<bool> StoreRefreshTokenAsync(string refreshToken, Guid userId, DateTime expiresAt);
        
        /// <summary>
        /// 리프레시 토큰에서 사용자 ID 추출
        /// </summary>
        /// <param name="refreshToken">사용자 ID를 추출할 리프레시 토큰</param>
        /// <returns>추출된 사용자 ID</returns>
        Task<Guid?> GetUserIdFromRefreshTokenAsync(string refreshToken);
        
        /// <summary>
        /// 리프레시 토큰 삭제
        /// </summary>
        /// <param name="refreshToken">삭제할 리프레시 토큰</param>
        /// <returns>삭제 성공 여부</returns>
        Task<bool> RemoveRefreshTokenAsync(string refreshToken);
        
        
        /// <summary>
        /// 리프레시 토큰 유효성 검사
        /// </summary>
        /// <param name="refreshToken">검사할 리프레시 토큰</param>
        /// <returns>유효성 검사 결과</returns>
        Task<bool> IsRefreshTokenValidAsync(string refreshToken);
    }
}
