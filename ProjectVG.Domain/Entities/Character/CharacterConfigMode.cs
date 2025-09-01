namespace ProjectVG.Domain.Entities.Characters
{
    /// <summary>
    /// 캐릭터 설정 모드
    /// </summary>
    public enum CharacterConfigMode
    {
        /// <summary>
        /// 개별 필드 설정 모드 (JSON 구조)
        /// </summary>
        Individual = 0,
        
        /// <summary>
        /// SystemPrompt 직접 입력 모드
        /// </summary>
        SystemPrompt = 1
    }
}