using ProjectVG.Application.Models.Chat;
using Microsoft.Extensions.Logging;
using ProjectVG.Application.Services.Chat.Preprocessors;

namespace ProjectVG.Application.Services.Chat.Core
{
    public class ChatPreprocessor
    {
        private readonly MemoryContextPreprocessor _memoryContextPreprocessor;
        private readonly ConversationHistoryPreprocessor _conversationHistoryPreprocessor;
        private readonly InstructionPreprocessor _instructionPreprocessor;
        private readonly ILogger<ChatPreprocessor> _logger;

        public ChatPreprocessor(
            MemoryContextPreprocessor memoryContextPreprocessor,
            ConversationHistoryPreprocessor conversationHistoryPreprocessor,
            InstructionPreprocessor instructionPreprocessor,
            ILogger<ChatPreprocessor> logger)
        {
            _memoryContextPreprocessor = memoryContextPreprocessor;
            _conversationHistoryPreprocessor = conversationHistoryPreprocessor;
            _instructionPreprocessor = instructionPreprocessor;
            _logger = logger;
        }

        public async Task<ChatPreprocessContext> PreprocessAsync(ProcessChatCommand command)
        {
            // 기억
            var memoryContext = await _memoryContextPreprocessor.CollectMemoryContextAsync(command.Message);
            
            // 대화 이력
            var conversationHistory = await _conversationHistoryPreprocessor.CollectConversationHistoryAsync(command.SessionId);

            // todo : 지정된 캐릭터 설정 불러오기
            string voiceName = "Haru";

            // todo : 지시사항 설정
            var allowedEmotions = _instructionPreprocessor.GetAllowedEmotions(voiceName);
            var instructions = _instructionPreprocessor.GetInstructions(allowedEmotions);

            // todo : 시스템 프롬포트 작성 (캐릭터 설정에 맞게 작성)
            return new ChatPreprocessContext {
                SessionId = command.SessionId,
                UserMessage = command.Message,
                Actor = command.Actor,
                Action = command.Action,
                CharacterId = command.CharacterId,
                MemoryContext = memoryContext,
                ConversationHistory = conversationHistory,
                SystemMessage = ProjectVG.Common.Constants.LLMSettings.Chat.SystemPrompt,
                Instructions = instructions,
                VoiceName = voiceName,
                AllowedEmotions = allowedEmotions
            };
        }
    }
}
