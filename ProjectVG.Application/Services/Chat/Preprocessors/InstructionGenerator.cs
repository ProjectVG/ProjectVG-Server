using ProjectVG.Common.Constants;

namespace ProjectVG.Application.Services.Chat.Preprocessors
{
    public class InstructionGenerator
    {
        public string Generate()
        {
            var baseInstructions = "netural 감정을 주로 사용(80%), 하나 또는 두 문장";
            var formatBlock = ChatOutputFormat.GetFormatInstructions();
            return $"{formatBlock}\n{baseInstructions}";
        }
    }
} 