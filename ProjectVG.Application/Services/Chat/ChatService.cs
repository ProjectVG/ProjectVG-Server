using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Application.Services.Chat.Processors;
using ProjectVG.Application.Services.Chat.Validators;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Character;
using Microsoft.Extensions.DependencyInjection;
using ProjectVG.Application.Models.Chat;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ChatService> _logger;

        private readonly IConversationService _conversationService;
        private readonly ICharacterService _characterService;

        private readonly ChatRequestValidator _validator;
        private readonly MemoryContextPreprocessor _memoryPreprocessor;
        private readonly UserInputAnalysisProcessor _inputProcessor;
        private readonly UserInputActionProcessor _actionProcessor;

        private readonly ChatLLMProcessor _llmProcessor;
        private readonly ChatTTSProcessor _ttsProcessor;
        private readonly ChatResultProcessor _resultProcessor;

        public ChatService(
            IServiceScopeFactory scopeFactory,
            ILogger<ChatService> logger,
            IConversationService conversationService,
            ICharacterService characterService,
            ChatRequestValidator validator,
            MemoryContextPreprocessor memoryPreprocessor,
            UserInputAnalysisProcessor inputProcessor,
            UserInputActionProcessor actionProcessor,
            ChatLLMProcessor llmProcessor,
            ChatTTSProcessor ttsProcessor,
            ChatResultProcessor resultProcessor
        ) {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _conversationService = conversationService;
            _characterService = characterService;
            _validator = validator;
            _memoryPreprocessor = memoryPreprocessor;
            _inputProcessor = inputProcessor;
            _actionProcessor = actionProcessor;

            _llmProcessor = llmProcessor;
            _ttsProcessor = ttsProcessor;
            _resultProcessor = resultProcessor;
        }

        public async Task<ChatRequestResponse> EnqueueChatRequestAsync(ProcessChatCommand command)
        {
            await _validator.ValidateAsync(command);

            var preprocessContext = await PrepareChatRequestAsync(command);

            _ = Task.Run(async () => {
                await ProcessChatRequestInternalAsync(preprocessContext);
            });

            return ChatRequestResponse.Accepted(command.SessionId, command.UserId, command.CharacterId);
        }


        /// <summary>
        /// 채팅 요청 준비
        /// </summary>
        private async Task<ChatPreprocessContext> PrepareChatRequestAsync(ProcessChatCommand command)
        {
            var characterDto = await _characterService.GetCharacterByIdAsync(command.CharacterId);
            command.SetCharacter(characterDto!);
            
            var conversationHistory = await _conversationService.GetConversationHistoryAsync(command.UserId, command.CharacterId, 10);

            var inputAnalysis = await _inputProcessor.ProcessAsync(command.Message, conversationHistory);
            await _actionProcessor.ProcessAsync(command, inputAnalysis);
            
            var memoryContext = await _memoryPreprocessor.CollectMemoryContextAsync(command.UserId.ToString(), command.Message, inputAnalysis);

            return new ChatPreprocessContext(
                command,
                memoryContext,
                conversationHistory
            );
        }

        /// <summary>
        /// 채팅 요청 처리
        /// </summary>
        private async Task ProcessChatRequestInternalAsync(ChatPreprocessContext context)
        {
            try {
                _logger.LogInformation("채팅 요청 처리 시작: {SessionId}", context.SessionId);

                // 작업 처리 단계: LLM -> TTS -> 결과 전송 + 저장
                var llmResult = await _llmProcessor.ProcessAsync(context);
                await _ttsProcessor.ProcessAsync(context, llmResult);
                
                using var scope = _scopeFactory.CreateScope();
                var resultProcessor = scope.ServiceProvider.GetRequiredService<ChatResultProcessor>();
                await resultProcessor.SendResultsAsync(context, llmResult);
                await resultProcessor.PersistResultsAsync(context, llmResult);

                _logger.LogInformation("채팅 요청 처리 완료: {SessionId}, 토큰: {TokensUsed}",
                    context.SessionId, llmResult.TokensUsed);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "채팅 요청 처리 중 오류 발생: {SessionId}", context.SessionId);
            }
        }
    }
}
