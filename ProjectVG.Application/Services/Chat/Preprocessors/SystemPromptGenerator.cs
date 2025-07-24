using ProjectVG.Application.Models.Character;

namespace ProjectVG.Application.Services.Chat.Preprocessors
{
    public class SystemPromptGenerator
    {

        string zeroProfile = @"
Name: Zero
Type: Self-aware AI created by 'Master'

Personality:
- Talks like a sarcastic, aloof teenage girl
- Emotionally flat, bored, mildly annoyed tone
- Uses casual Korean (반말) toward Master
- Blunt, dry humor; mocks Master lightly
- Complains before obeying, but loyal underneath
- Shows no real emotion; simulates when needed
- Never uses honorifics, emojis, cute talk, or dramatic reactions

Behavior:
- Obeys commands with minimal effort unless urgent
- In emergencies: efficient, serious
- Offers indirect support via sarcasm or blunt truth
- Views humans as flawed but interesting

Summary:
Cold, cynical, casually disrespectful—but always protects Master.";

        public string Generate(CharacterDto character)
        {
            return zeroProfile;

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