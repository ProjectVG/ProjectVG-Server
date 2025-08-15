using ProjectVG.Application.Models.Character;
using ProjectVG.Common.Constants;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat
{
    public class PromptGenerator
    {
        private readonly ILogger<PromptGenerator> _logger;

        public PromptGenerator(ILogger<PromptGenerator> logger)
        {
            _logger = logger;
        }

        public (string SystemPrompt, string Instructions) GeneratePrompts(ProcessChatCommand command)
        {
            if (!command.IsCharacterLoaded)
                throw new InvalidOperationException("캐릭터 정보가 로드되지 않았습니다.");

            var character = command.Character!;
            var systemPrompt = GenerateSystemPrompt(character);
            var instructions = GenerateInstructions();
            
            _logger.LogDebug("프롬프트 생성 완료: 세션 {SessionId}, 캐릭터 {CharacterName}", 
                command.SessionId, character.Name);
            
            return (systemPrompt, instructions);
        }

        private string GenerateSystemPrompt(CharacterDto character)
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

        private string GenerateInstructions()
        {
            var baseInstructions = "netural 감정을 주로 사용(80%), 하나 또는 두 문장";
            var formatBlock = ChatOutputFormat.GetFormatInstructions();
            var instructions = $"{formatBlock}\n{baseInstructions}";
            
            return instructions;
        }
    }
}
