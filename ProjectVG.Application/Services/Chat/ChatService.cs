using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Character;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using ProjectVG.Application.Services.Chat.Preprocessors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ChatRequestValidator _validator;
        private readonly ChatLLMProcessor _llmProcessor;
        private readonly ChatTTSProcessor _ttsProcessor;
        private readonly ILogger<ChatService> _logger;
        
        // 전처리 관련 의존성들
        private readonly IMemoryClient _memoryClient;
        private readonly IConversationService _conversationService;
        private readonly ICharacterService _characterService;
        private readonly MemoryContextPreprocessor _memoryPreprocessor;
        private readonly ConversationHistoryPreprocessor _conversationPreprocessor;
        private readonly PromptGenerator _promptGenerator;

        public ChatService(
            IServiceScopeFactory scopeFactory,
            ChatRequestValidator validator,
            ChatLLMProcessor llmProcessor,
            ChatTTSProcessor ttsProcessor,
            ILogger<ChatService> logger,
            IMemoryClient memoryClient,
            IConversationService conversationService,
            ICharacterService characterService,
            MemoryContextPreprocessor memoryPreprocessor,
            ConversationHistoryPreprocessor conversationPreprocessor,
            PromptGenerator promptGenerator)
        {
            _scopeFactory = scopeFactory;
            _validator = validator;
            _llmProcessor = llmProcessor;
            _ttsProcessor = ttsProcessor;
            _logger = logger;
            _memoryClient = memoryClient;
            _conversationService = conversationService;
            _characterService = characterService;
            _memoryPreprocessor = memoryPreprocessor;
            _conversationPreprocessor = conversationPreprocessor;
            _promptGenerator = promptGenerator;
        }

        public async Task<ChatRequestResponse> EnqueueChatRequestAsync(ProcessChatCommand command)
        {
            var validationResult = await _validator.ValidateAsync(command);
            if (!validationResult.IsValid)
            {
                return ChatRequestResponse.Rejected(validationResult.ErrorMessage, validationResult.ErrorCode, command.SessionId, command.UserId, command.CharacterId);
            }

            if (!command.IsCharacterLoaded)
            {
                var characterDto = await _characterService.GetCharacterByIdAsync(command.CharacterId);
                command.SetCharacter(characterDto);
            }

            var preprocessContext = await PrepareChatRequestAsync(command);
            if (!preprocessContext.IsValid)
            {
                return preprocessContext.RequestResponse;
            }

            _ = Task.Run(async () =>
            {
                using var processScope = _scopeFactory.CreateScope();
                await ProcessChatRequestInternalAsync(preprocessContext.Context, processScope.ServiceProvider);
            });

            return ChatRequestResponse.Accepted(command.SessionId, command.UserId, command.CharacterId);
        }

        private async Task<ChatPreprocessResult> PrepareChatRequestAsync(ProcessChatCommand command)
        {
            try
            {
                var memoryContext = await _memoryPreprocessor.CollectMemoryContextAsync(command.UserId.ToString(), command.Message);
                var conversationHistory = await _conversationPreprocessor.CollectConversationHistoryAsync(command.UserId, command.CharacterId);
                
                var (systemMessage, instructions) = _promptGenerator.GeneratePrompts(command);

                var context = new ChatPreprocessContext(
                    command,
                    systemMessage,
                    instructions,
                    memoryContext,
                    conversationHistory
                );

                _logger.LogDebug("채팅 요청 전처리 완료: 세션 {SessionId}, 메모리 컨텍스트 {MemoryCount}개, 대화 기록 {HistoryCount}개", 
                    command.SessionId, memoryContext.Count, conversationHistory.Count);

                return ChatPreprocessResult.Success(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 요청 전처리 중 오류 발생: {SessionId}", command.SessionId);
                return ChatPreprocessResult.Failure(ChatRequestResponse.Rejected("전처리 중 오류가 발생했습니다.", "PREPROCESS_ERROR", command.SessionId, command.UserId, command.CharacterId));
            }
        }

        private async Task ProcessChatRequestInternalAsync(ChatPreprocessContext context, IServiceProvider services)
        {
            try
            {
                _logger.LogInformation("채팅 요청 처리 시작: {SessionId}", context.SessionId);

                var resultProcessor = services.GetRequiredService<ChatResultProcessor>();

                // 작업 처리 단계: LLM -> TTS -> 결과 전송 + 저장
                var llmResult = await _llmProcessor.ProcessAsync(context);
                await _ttsProcessor.ProcessAsync(context, llmResult);
                await resultProcessor.SendResultsAsync(context, llmResult);
                await resultProcessor.PersistResultsAsync(context, llmResult);

                _logger.LogInformation("채팅 요청 처리 완료: {SessionId}, 토큰: {TokensUsed}",
                    context.SessionId, llmResult.TokensUsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 요청 처리 중 오류 발생: {SessionId}", context.SessionId);
            }
        }
    }
} 