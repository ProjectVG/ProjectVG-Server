using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Character;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ProjectVG.Application.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ChatRequestValidator _validator;
        private readonly UserInputAnalysisProcessor _inputProcessor;
        private readonly ChatLLMProcessor _llmProcessor;
        private readonly ChatTTSProcessor _ttsProcessor;
        private readonly ChatResultProcessor _resultProcessor;
        private readonly ILogger<ChatService> _logger;
        
        // 전처리 관련 의존성들
        private readonly IMemoryClient _memoryClient;
        private readonly IConversationService _conversationService;
        private readonly ICharacterService _characterService;
        private readonly MemoryContextPreprocessor _memoryPreprocessor;
        private readonly ConversationHistoryPreprocessor _conversationPreprocessor;

        public ChatService(
            IServiceScopeFactory scopeFactory,
            ChatRequestValidator validator,
            UserInputAnalysisProcessor inputProcessor,
            ChatLLMProcessor llmProcessor,
            ChatTTSProcessor ttsProcessor,
            ChatResultProcessor resultProcessor,
            ILogger<ChatService> logger,
            IMemoryClient memoryClient,
            IConversationService conversationService,
            ICharacterService characterService,
            MemoryContextPreprocessor memoryPreprocessor,
            ConversationHistoryPreprocessor conversationPreprocessor)
        {
            _scopeFactory = scopeFactory;
            _validator = validator;
            _inputProcessor = inputProcessor;
            _llmProcessor = llmProcessor;
            _ttsProcessor = ttsProcessor;
            _resultProcessor = resultProcessor;
            _logger = logger;
            _memoryClient = memoryClient;
            _conversationService = conversationService;
            _characterService = characterService;
            _memoryPreprocessor = memoryPreprocessor;
            _conversationPreprocessor = conversationPreprocessor;
        }

        public async Task<ChatRequestResponse> EnqueueChatRequestAsync(ProcessChatCommand command)
        {
            var validationResult = await _validator.ValidateAsync(command);
            if (!validationResult.IsValid)
            {
                return ChatRequestResponse.Rejected(validationResult.ErrorMessage, validationResult.ErrorCode, command.SessionId, command.UserId, command.CharacterId);
            }

            var preprocessContext = await PrepareChatRequestAsync(command);
            if (!preprocessContext.IsValid)
            {
                return preprocessContext.RequestResponse;
            }

            _ = Task.Run(async () =>
            {
                using var processScope = _scopeFactory.CreateScope();
                await ProcessChatRequestInternalAsync(preprocessContext.Context!);
            });

            return ChatRequestResponse.Accepted(command.SessionId, command.UserId, command.CharacterId);
        }

        private async Task<ChatPreprocessResult> PrepareChatRequestAsync(ProcessChatCommand command)
        {
            try
            {
                var characterDto = await _characterService.GetCharacterByIdAsync(command.CharacterId);
                command.SetCharacter(characterDto!);

                // 대화 기록을 먼저 가져와서 공유
                var conversationHistory = await _conversationService.GetConversationHistoryAsync(command.UserId, command.CharacterId, 10);
                var recentConversations = conversationHistory.Take(5).ToList();

                // 사용자 입력 분석 (대화 기록 전달)
                /*
                var inputAnalysis = await _inputProcessor.ProcessAsync(command.Message, recentConversations);
                if (!inputAnalysis.IsValid)
                {
                    return ChatPreprocessResult.Failure(ChatRequestResponse.Rejected(
                        inputAnalysis.RejectionReason ?? "부적절한 입력입니다.", 
                        "INVALID_INPUT", 
                        command.SessionId, 
                        command.UserId, 
                        command.CharacterId));
                }
                */

                // 분석 결과를 기반으로 메모리 컨텍스트 수집
                var memoryContext = await _memoryPreprocessor.CollectMemoryContextAsync(command.UserId.ToString(), command.Message, null);
                
                // 대화 기록은 이미 가져왔으므로 직접 사용
                var conversationHistoryList = conversationHistory.Select(c => $"{c.Role}: {c.Content}").ToList();

                var context = new ChatPreprocessContext(
                    command,
                    memoryContext,
                    conversationHistoryList
                );

                _logger.LogDebug("채팅 요청 전처리 완료: 세션 {SessionId}, 메모리 컨텍스트 {MemoryCount}개, 대화 기록 {HistoryCount}개", 
                    command.SessionId, memoryContext.Count, conversationHistoryList.Count);

                return ChatPreprocessResult.Success(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "채팅 요청 전처리 중 오류 발생: {SessionId}", command.SessionId);
                return ChatPreprocessResult.Failure(ChatRequestResponse.Rejected("전처리 중 오류가 발생했습니다.", "PREPROCESS_ERROR", command.SessionId, command.UserId, command.CharacterId));
            }
        }

        private async Task ProcessChatRequestInternalAsync(ChatPreprocessContext context)
        {
            try
            {
                _logger.LogInformation("채팅 요청 처리 시작: {SessionId}", context.SessionId);

                // 작업 처리 단계: LLM -> TTS -> 결과 전송 + 저장
                var llmResult = await _llmProcessor.ProcessAsync(context);
                await _ttsProcessor.ProcessAsync(context, llmResult);
                await _resultProcessor.SendResultsAsync(context, llmResult);
                await _resultProcessor.PersistResultsAsync(context, llmResult);

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