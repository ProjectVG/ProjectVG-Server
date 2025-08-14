using Microsoft.Extensions.Logging;
using ProjectVG.Application.Models.Character;
using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Character;
using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Domain.Entities.Users;

namespace ProjectVG.Application.Services.Chat.Core
{
    public class ChatPreprocessor
    {
        private readonly MemoryContextPreprocessor _memoryContextPreprocessor;
        private readonly ConversationHistoryPreprocessor _conversationHistoryPreprocessor;
        private readonly ICharacterService _characterService;
        private readonly SystemPromptGenerator _systemPromptGenerator;
        private readonly InstructionGenerator _instructionGenerator;
        private readonly ILogger<ChatPreprocessor> _logger;

        public ChatPreprocessor(
            MemoryContextPreprocessor memoryContextPreprocessor,
            ConversationHistoryPreprocessor conversationHistoryPreprocessor,
            ICharacterService characterService,
            SystemPromptGenerator systemPromptGenerator,
            InstructionGenerator instructionGenerator,
            ILogger<ChatPreprocessor> logger)
        {
            _memoryContextPreprocessor = memoryContextPreprocessor;
            _conversationHistoryPreprocessor = conversationHistoryPreprocessor;
            _characterService = characterService;
            _systemPromptGenerator = systemPromptGenerator;
            _instructionGenerator = instructionGenerator;
            _logger = logger;
        }

        public async Task<ChatPreprocessContext> PreprocessAsync(ProcessChatCommand command)
        {
            // 기억
            var memoryContext = await _memoryContextPreprocessor
                .CollectMemoryContextAsync(command.UserId.ToString(), command.Message);
            
            // 대화 이력
            var conversationHistory = await _conversationHistoryPreprocessor
                .CollectConversationHistoryAsync(command.UserId, command.CharacterId);

            // todo : 지정된 캐릭터 설정 불러오기
            CharacterDto? characterDto = await _characterService.GetCharacterByIdAsync(command.CharacterId);

            // 프롬포트 설정
            var systemMessage = _systemPromptGenerator.Generate(characterDto);
            var instructions = _instructionGenerator.Generate();

            var context = new ChatPreprocessContext {
                SessionId = command.SessionId,
                UserId = command.UserId,
                CharacterId = command.CharacterId,
                SystemMessage = systemMessage,
                Instructions = instructions,
                UserMessage = command.Message,
                MemoryStore = command.UserId.ToString(),
                Action = command.Action,
                MemoryContext = memoryContext,
                ConversationHistory = conversationHistory,
                VoiceName = characterDto.VoiceId,
            };

            // 컨텍스트 내용 출력
            _logger.LogInformation("Preprocess 완료: {Context}", context);
            _logger.LogInformation("상세 정보:\n{DetailedInfo}", context.GetDetailedInfo());

            return context;
        }
    }
}
