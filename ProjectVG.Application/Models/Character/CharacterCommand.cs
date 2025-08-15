namespace ProjectVG.Application.Models.Character
{
    /// <summary>
    /// 캐릭터 명령 기본 구조 (내부 비즈니스 로직용)
    /// </summary>
    public class CharacterCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
