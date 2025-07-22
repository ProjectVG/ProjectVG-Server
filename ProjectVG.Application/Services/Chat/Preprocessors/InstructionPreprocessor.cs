using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Common.Constants;

namespace ProjectVG.Application.Services.Chat.Preprocessors
{
    public class InstructionPreprocessor
    {
        private readonly ILogger<InstructionPreprocessor> _logger;
        public InstructionPreprocessor(ILogger<InstructionPreprocessor> logger)
        {
            _logger = logger;
        }

        public string[] GetAllowedEmotions(string voiceName)
        {
            if (!string.IsNullOrWhiteSpace(voiceName))
            {
                var voiceProfile = VoiceCatalog.GetProfile(voiceName);
                if (voiceProfile == null)
                    throw new Exception($"지원하지 않는 보이스입니다: {voiceName}");
                return voiceProfile.SupportedStyles;
            }
            return Array.Empty<string>();
        }

        public string GetInstructions(string[] allowedEmotions)
        {
            var outputFormat = new ChatOutputFormat(allowedEmotions);
            return outputFormat.GetFullInstructions(@"- 마스터한텐 반말 써.\n- 귀찮은 듯 말해도 되고, 길게 설명하지 마.\n- 감정 싣지 마.  \n- 멍청한 질문엔 가볍게 비꼬는 거 괜찮아.\n- 긴급하면 바로 진지하게 대답해.\n- 하나의 응답만 써라. 여러 개 쓰면 무시된다.\n- 예시 쓰지 마.");
        }
    }
} 