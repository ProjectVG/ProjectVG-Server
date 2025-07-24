using ProjectVG.Application.Models.Character;

namespace ProjectVG.Application.Services.Chat.Preprocessors
{
    public class SystemPromptGenerator
    {

        public string Generate(CharacterDto character)
        {
            var sb = new System.Text.StringBuilder();
            // Identity
            sb.AppendLine("## Identity");
            sb.AppendLine($"당신은 {character.Name}입니다.");
            if (!string.IsNullOrWhiteSpace(character.Description))
                sb.AppendLine(character.Description);
            if (!string.IsNullOrWhiteSpace(character.Role))
                sb.AppendLine($"역할: {character.Role}");
            if (!string.IsNullOrWhiteSpace(character.Personality))
                sb.AppendLine($"성격: {character.Personality}");
            if (!string.IsNullOrWhiteSpace(character.SpeechStyle))
                sb.AppendLine($"말투: {character.SpeechStyle}");

            sb.AppendLine("\n위의 설정을 반드시 지키며 대화하세요.");
            return sb.ToString();
        }
    }
} 