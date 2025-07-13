namespace ProjectVG.Application.Models.Character
{
    /// <summary>
    /// 캐릭터 생성 명령 (내부 비즈니스 로직용)
    /// </summary>
    public class CreateCharacterCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
} 