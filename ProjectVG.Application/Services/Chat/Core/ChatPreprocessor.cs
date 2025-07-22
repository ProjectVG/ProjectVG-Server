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
            var memoryContext = await _memoryContextPreprocessor.CollectMemoryContextAsync(command.Message);
            var conversationHistory = await _conversationHistoryPreprocessor.CollectConversationHistoryAsync(command.SessionId);

            string voiceName = "Haru";
            var allowedEmotions = _instructionPreprocessor.GetAllowedEmotions(voiceName);
            var instructions = _instructionPreprocessor.GetInstructions(allowedEmotions);

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
