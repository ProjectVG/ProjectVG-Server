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
        /// <summary>
/// 리프레시 토큰을 지정된 사용자와 만료 시각과 함께 저장한다.
/// </summary>
/// <param name="refreshToken">저장할 리프레시 토큰 문자열.</param>
/// <param name="userId">토큰에 연관된 사용자 식별자.</param>
/// <param name="expiresAt">토큰의 만료 시각.</param>
/// <returns>저장에 성공하면 true, 실패하면 false.</returns>
        Task<bool> StoreRefreshTokenAsync(string refreshToken, Guid userId, DateTime expiresAt);
        
        /// <summary>
        /// 리프레시 토큰에서 사용자 ID 추출
        /// </summary>
        /// <param name="refreshToken">사용자 ID를 추출할 리프레시 토큰</param>
        /// <summary>
/// 주어진 리프레시 토큰에 연결된 사용자 ID를 비동기적으로 조회합니다.
/// </summary>
/// <param name="refreshToken">조회할 리프레시 토큰</param>
/// <returns>토큰에 연결된 사용자 ID를 반환합니다. 토큰이 존재하지 않거나 유효하지 않은 경우 null을 반환합니다.</returns>
        Task<Guid?> GetUserIdFromRefreshTokenAsync(string refreshToken);
        
        /// <summary>
        /// 리프레시 토큰 삭제
        /// </summary>
        /// <param name="refreshToken">삭제할 리프레시 토큰</param>
        /// <summary>
/// 지정된 리프레시 토큰을 저장소에서 제거합니다.
/// </summary>
/// <param name="refreshToken">제거할 리프레시 토큰 문자열.</param>
/// <returns>토큰이 존재하여 성공적으로 제거되면 true, 그렇지 않으면 false를 반환합니다.</returns>
        Task<bool> RemoveRefreshTokenAsync(string refreshToken);
        
        
        /// <summary>
        /// 리프레시 토큰 유효성 검사
        /// </summary>
        /// <param name="refreshToken">검사할 리프레시 토큰</param>
        /// <summary>
/// 저장소에 있는 지정된 리프레시 토큰이 유효한지 비동기적으로 확인합니다.
/// </summary>
/// <param name="refreshToken">검사할 리프레시 토큰 문자열.</param>
/// <returns>
/// 토큰이 저장되어 있고 만료되지 않은 경우에만 <c>true</c>를 반환합니다. 그렇지 않으면 <c>false</c>를返します.
/// </returns>
        Task<bool> IsRefreshTokenValidAsync(string refreshToken);

        /// <summary>
        /// 리프레시 토큰의 만료 시간 조회
        /// </summary>
        /// <param name="refreshToken">만료 시간을 조회할 리프레시 토큰</param>
        /// <summary>
/// 지정된 리프레시 토큰의 만료 시각을 비동기적으로 조회합니다.
/// </summary>
/// <param name="refreshToken">만료 시각을 조회할 리프레시 토큰 문자열.</param>
/// <returns>토큰의 만료 시각(DateTime) 또는 토큰이 존재하지 않거나 만료 정보가 없으면 null을 반환합니다.</returns>
        Task<DateTime?> GetRefreshTokenExpiresAtAsync(string refreshToken);
    }
}
