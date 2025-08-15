using ProjectVG.Application.Models.Chat;
using ProjectVG.Application.Services.Chat.Preprocessors;
using ProjectVG.Application.Services.Conversation;
using ProjectVG.Application.Services.Character;
using ProjectVG.Infrastructure.Integrations.MemoryClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectVG.Domain.Enums;

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
                var inputAnalysis = await _inputProcessor.ProcessAsync(command.Message, recentConversations);
                
                // Action별 처리
                var actionResult = await HandleUserInputActionAsync(command, inputAnalysis);
                if (actionResult != null)
                {
                    return actionResult;
                }

                // 분석 결과를 기반으로 메모리 컨텍스트 수집
                var memoryContext = await _memoryPreprocessor.CollectMemoryContextAsync(command.UserId.ToString(), command.Message, inputAnalysis);
                
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

        private async Task<ChatPreprocessResult?> HandleUserInputActionAsync(ProcessChatCommand command, UserInputAnalysis inputAnalysis)
        {
            switch (inputAnalysis.Action)
            {
                case UserInputAction.Ignore:
                    // 무시: 즉시 프로세스 종료, HTTP 반환
                    _logger.LogInformation("입력 무시: {Message}", command.Message);
                    return ChatPreprocessResult.Failure(ChatRequestResponse.Rejected(
                        inputAnalysis.FailureReason ?? "잘못된 입력입니다.", 
                        "IGNORE_INPUT", 
                        command.SessionId, 
                        command.UserId, 
                        command.CharacterId));

                case UserInputAction.Reject:
                    // 거절: 캐시된 대화 사용, 대화 내용 저장 후 종료
                    _logger.LogInformation("입력 거절: {Message}, 사유: {Reason}", 
                        command.Message, inputAnalysis.FailureReason);
                    
                    // 대화 내용 저장 (사용자 입력만)
                    await _conversationService.AddMessageAsync(command.UserId, command.CharacterId, ChatRole.User, command.Message);
                    
                    return ChatPreprocessResult.Failure(ChatRequestResponse.Rejected(
                        inputAnalysis.FailureReason ?? "부적절한 요청입니다.", 
                        "REJECT_INPUT", 
                        command.SessionId, 
                        command.UserId, 
                        command.CharacterId));



                case UserInputAction.Chat:
                    // 대화: 정상적인 처리 계속
                    _logger.LogDebug("정상 대화 처리: {Message}", command.Message);
                    break;

                case UserInputAction.Undefined:
                    // 미정: 현재는 대화로 처리 (향후 확장 가능)
                    _logger.LogInformation("미정 액션을 대화로 처리: {Message}", command.Message);
                    break;

                default:
                    _logger.LogWarning("알 수 없는 액션: {Action}, 메시지: {Message}", 
                        inputAnalysis.Action, command.Message);
                    return ChatPreprocessResult.Failure(ChatRequestResponse.Rejected(
                        "알 수 없는 액션입니다.", 
                        "UNKNOWN_ACTION", 
                        command.SessionId, 
                        command.UserId, 
                        command.CharacterId));
            }
            return null; // 해당 액션에 대한 처리가 없으면 null 반환
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